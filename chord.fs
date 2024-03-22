open System

type PeerMessage =
    | Create of (int * IActorRef)
    | Join of (int * IActorRef * int * IActorRef)
    | Lookup of (int * int * IActorRef)
    | AddInFingerTable of int
    | Done

type SimulatorMessage =
    | CreateChord
    | JoinChord
    | MakeFingerTable
    | Lookups
    | FoundKey of (IActorRef * int * int * IActorRef)

let systemRef = ActorSystem.Create("System")
let mutable numNodes = fsi.CommandLineArgs.[1] |> int
let mutable numRequests = fsi.CommandLineArgs.[2] |> int
let mutable m = 0

let generateHash(key: int) =
    let bytesArr = BitConverter.GetBytes(key)
    let hash = HashAlgorithm.Create("SHA1").ComputeHash(bytesArr)
    BitConverter.ToString(hash).Replace("-", "").ToLower()

let mutable peers = [||]
let mutable flag = true

let peer (id: int) (mailbox:Actor<_>) =
    let mutable PeerId: int = id
    let mutable Successor: (int * IActorRef)=(-1, null)
    let mutable Predecessor: (int * IActorRef)=(-1, null)
    let mutable SimulatorReference: IActorRef = null
    let mutable FingerTable: (int * IActorRef) []=[||]
    
    let rec loop state = actor {
        let! message = mailbox.Receive()
        match message with
        | Create (id,master)-> 
            SimulatorReference <- master
            PeerId <- id
        | Join (suci, sucR, prei, preR)->
            Predecessor <- (prei, preR)
            Successor <- (suci, sucR)
        | Lookup (key, hops, requestor) ->
            let mutable hopCount = hops
            let mutable ftfound = false
            if flag && key = PeerId  then 
                flag <- false
                SimulatorReference <! FoundKey((snd Successor), key, hopCount, requestor)
                mailbox.Self <! Done
            elif (key <= (fst Successor)) && (key > PeerId) then
                flag <- false
                SimulatorReference <! FoundKey((snd Successor), key, hopCount, requestor)
                mailbox.Self <! Done
            else
                if  flag && key < PeerId  then 
                    hopCount <- hopCount + 1 
                    flag <- false
                    SimulatorReference <! FoundKey((snd (Array.get peers (0))), key, hopCount, requestor)
                    mailbox.Self <! Done
                else 
                    let mutable i = m - 1
                    while flag &&  i > 0 do
                        let fingerValue = (fst (Array.get FingerTable i))
                        if  flag && fingerValue < PeerId then
                            flag <- false
                            hopCount <- hopCount+1
                            SimulatorReference <! FoundKey((snd (Array.get peers (0))), key, hopCount, requestor)
                        elif flag && (fingerValue <= key && fingerValue > PeerId)  then
                            ftfound <- true
                            hopCount <- hopCount + 1
                            (snd (Array.get FingerTable i)) <! Lookup(key, hopCount, requestor)
                        i <- 0
                        Threading.Thread.Sleep(400)
                        hopCount <- hopCount + 1
                        (snd (Array.get FingerTable (m - 1))) <! Lookup(key, hopCount, requestor)
            mailbox.Self <! Done
        | AddInFingerTable(id) ->
            for i in 0..m - 1 do 
                let mutable ft = false
                for j in 0..peers.Length - 2 do 
                    let ele = (fst (Array.get peers j))
                    let scnd = (fst (Array.get peers (j+1)))
                    if ((id + (pown 2 i)) > ele) && ((id + (pown 2 i)) <= scnd) then
                        FingerTable <- Array.append FingerTable [|(scnd , snd (Array.get peers (j+1)))|]
                        ft <- true
                if not ft  then
                    FingerTable <- Array.append FingerTable [|(fst (Array.get peers (0)), snd (Array.get peers (0)))|]
        | Done -> printf "" |> ignore
        | _ ->  failwith "[ERR] Unknown message." |> ignore
        return! loop()
    }
    loop ()

let mutable sum = 0
let mutable average = 0.0

let simulator (numNodes: int) (mailbox:Actor<_>) =
    let rec loop state = actor {
        let! message = mailbox.Receive()
        let mutable maxid = 0
        let r = Random()
        match message with
        | CreateChord ->
            m <- (Math.Log2(float numNodes) |> int)
            for i in 1..numNodes do
                let id = r.Next(maxid + 1, (maxid + (Math.Log (numNodes |> float, 2.0)  |> int)))
                maxid <- id
                let node  = spawn systemRef ("Node" + (generateHash id)) (peer(maxid))
                peers <- Array.append peers [|(maxid, node)|]
                node <! Create (maxid, mailbox.Self)
            mailbox.Self <! JoinChord
        | JoinChord ->
            for i in 0..numNodes - 1 do 
                let mutable successori = i + 1
                let mutable predecessori = i - 1
                let node = Array.get peers i
                if i = (numNodes - 1) then 
                    successori <- 0
                if i = 0 then 
                    predecessori <- (numNodes - 1)
                let sucR = Array.get peers (successori)
                let preR = Array.get peers (predecessori)
                snd node <! Join (fst sucR, snd sucR, fst preR, snd preR)
                Threading.Thread.Sleep(150)
            mailbox.Self <! MakeFingerTable
        | MakeFingerTable ->
            for i in 0..numNodes - 1 do 
                let x = Array.get peers (i)
                snd x <! AddInFingerTable(fst x)
                Threading.Thread.Sleep(150)
            mailbox.Self <! Lookups 
        | Lookups ->
            let mutable key = 0
            for i in 0..numNodes - 1 do
                key <- key + (fst (Array.get peers i))
            key <- key / peers.Length
            key <- key |> int
            for i in 0..numNodes - 1 do
                for j in 1..(numRequests) do
                    (snd (Array.get peers i)) <!  Lookup (fst(Array.get peers (numNodes - 1)) - 1, 0, (snd (Array.get peers i)))
                    flag <- true
                    Threading.Thread.Sleep(400)
        | FoundKey (ref, key, hopCount, requestor) ->
            sum <- sum + hopCount
            average <- (sum |> float) / (numNodes  * (numRequests) |> float)  
            printfn "Input %A:Set key%A on node %A with number of hops = %A , Average:%A" requestor key ref hopCount average
        | _ ->  failwith "[ERR] Unknown message."
        return! loop()
    }
    loop ()
let simulatorRef = spawn systemRef "simulator" (simulator(numNodes)) 
simulatorRef <! CreateChord
Threading.Thread.Sleep(9000)
Threading.Thread.Sleep(9000)
systemRef.WhenTerminated.Wait()
