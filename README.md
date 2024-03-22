## How to run
Command:
`dotnet fsi Program.fsx <num_of_nodes> <num_of_requests>`

Usage:
`dotnet fsi Program.fsx 5 10`

## What is working
- The code uses Actor model to simulate the chord protocol and P2P system.
- There are 2 actors –
    1.	Peer – representing a node in the chord network
    2.	Simulator – for maintaining the statistics of the experiment and simulating the system
- The working is as follows:
    1.	The simulator creates a chord by adding nodes to the zeroth node one a time.
    2.	The zeroth node runs the lookup for new nodes ID and the new sets its successor to the resulting node of lookup.
    3.	When a node is added, stabilize is triggered to adjust the successors and predecessors of the neighbouring nodes.
    4.	Once the chord is stabilized, the finger tables are updated by the regular triggering of AddInFingerTable function.
    5.	When all the nodes are added, the chord formation is completed.
    6.	Then each node starts to send the lookup requests at random. Once the lookup output is found, it is sent to the simulator.
    7.	Once each peer has sent the defined no. of lookup outputs, simulator ends the program.

## Screenshot
For 5 nodes and 3 requests.
![alt text](https://github.com/haxxorsid/chord-protocol/blob/main/images/img1.png "nodes and 5 requests")
 
## What is the largest network you managed to deal with
Largest network we evaluated was of 1000 nodes with 10 requests. Average number of hops = 10.3769.

## Results
Plot-

![alt text](https://github.com/haxxorsid/chord-protocol/blob/main/images/img2.jpg "plot")

Tabular-

![alt text](https://github.com/haxxorsid/chord-protocol/blob/main/images/img3.jpg "tabular")