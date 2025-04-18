using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System;

int shard = 1;
int MaxLengthDivisor = 100;
if(args.Length>0){
    shard = int.Parse(args[0]);
    MaxLengthDivisor = int.Parse(args[1]);
}

// NodeGen nodeGen = new();
Dictionary<int, List<NodeGen.PathInfo>> goodSeeds = new();


int minLength = ShardDatabase.GetCurrentShardData(shard).depth+1;
int searchRange= int.MaxValue/MaxLengthDivisor;


List<int> searchItems = new(){0, 27, 4, 28};         // 8 blood engine, 35 impact activated
// Console.WriteLine(searchItems.Count);
for (int i = 0; i<searchRange;i++){
    
    int length = NodeGen.genNodes(i, ShardDatabase.GetCurrentShardData().depth, searchItems, minLength, ref goodSeeds);
    
    minLength=Math.Min(minLength, length);
    if(i%MathF.Ceiling(searchRange/1000)==0){
        Console.WriteLine(i+"/"+searchRange+" "+((float)i/searchRange)*100+"% Found "+goodSeeds.Keys.Count+" Seeds");
    }
    
}

Console.WriteLine(goodSeeds.Keys.Count);

foreach(int seed in goodSeeds.Keys){
    Console.Write(seed+": ");
    foreach(NodeGen.PathInfo cPath in goodSeeds[seed]){
            // Console.WriteLine("Path Length: "+l);
            foreach(Node n in cPath.Path){
                Console.Write(n.nodeType+" "+n.id+" -> ");
            }
            Console.WriteLine();
        }
}
foreach (int seed in goodSeeds.Keys){
    Console.Write(seed+": ");
    if(searchItems.Count>0){
        foreach(NodeGen.PathInfo cPath in goodSeeds[seed]){
            if(cPath.ItemHitInfos.Count>0){
                Console.Write(cPath.ItemHitInfos[0].rarity+": ");
                foreach(NodeGen.ItemHitInfo cInfo in cPath.ItemHitInfos){
                    
                    Console.Write(cInfo.nodeType+" "+cInfo.depth+" ");
                }
                    Console.Write("Or: ");
            }else{
                Console.WriteLine(seed+": erm valid seed yet empty items??? investigate.");
            }
        }
    }
}
Console.WriteLine();
Console.WriteLine(minLength);


//342225
//501643
//707440
//1980832


//Investigate:
//167109


//3650580

//Good seeds:
// 26490
// 258776
// See https://aka.ms/new-console-template for more information
public enum NodeType{
    Default,
    Shop,
    Challenge,
    Encounter,
    Boss,
    RestStop
}

public class Node{
    public int depth;
    public NodeType nodeType;
    public int id;
    public Vector3 realSpace;
    public List<Node> connections;
    public void setType(NodeType type) { this.nodeType = type;}
}
public class NodeGen{


    public static int genNodes(int seed,int depth, List<int> items, int minLength, ref Dictionary<int,List<PathInfo>> dict){
        List<Node> nodes = new List<Node>();
        //Set Depth from rundata.runconfig.nrOfLevels found in configs of world shards (shard 1 is 12)
        // int depth = 12;
        //get seed -> setup seeded random
        // int seed = -1;
        System.Random random= new Random(seed);

        //pass random to LevelSelectionMapGenerator.Generate(generationInfo, random)
        //                                                     ^mainly just that depth value

        Node nNode = new Node();
        nNode.depth = 0;
        nNode.nodeType = NodeType.Default;
        nNode.id=0;
        nNode.realSpace = new Vector3(0,10,0);
        nNode.connections = new List<Node>();
        nodes.Add(nNode);
        for(int m =1; m<=depth;m++){
            int num = random.Next(2, 4);
            for(int j =0; j<num; j++){
                for (int k = 0; k<15; k++){
                    Vector3 targetLoc = new Vector3(random.Range(-20f, 20f), 10f, random.Range(0f, 2f)+(float)(m*7));
                    if(IsValidPosition(targetLoc, nodes)){
                        Node n = new Node();
                        n.depth = m;
                        n.nodeType = NodeType.Default;
                        n.id = nodes.Count;
                        n.realSpace = targetLoc;
                        n.connections = new List<Node>();
                        nodes.Add(n);
                        break;
                    }
                    
                }
            }
        }
        
        Node from = nodes[0];

        foreach(Node to in nodes.FindAll((Node x) => x.depth==1)){
            from.connections.Add(to);
        }

        int i;
        int i2;
        for(i =1; i<depth; i=i2+1){
            List<Node> list = nodes.FindAll((Node x) => x.depth==i);
            List<Node> list2 = nodes.FindAll((Node x) => x.depth==i+1);

            int count = 0;
            
            foreach(Node n in list){
                IEnumerable<Node> nodesByClose = GetNodesSortedByClose(list2, n.realSpace);
                bool flag =false;
                foreach(Node n2 in nodesByClose){
                    if(!DoesPathsIntersect(n.realSpace, n2.realSpace,nodes)){
                        // Console.WriteLine("At Conn Attempt 1 adding conn"+ n.id+" "+n2.id);
                        n.connections.Add(n2);
                        flag = true;
                        break;
                    }
                    
                }
                if(!flag){
                    Node toAdd = random.Choice(list2);
                    // Console.WriteLine("failsafe 1 adding conn"+n.id+" "+toAdd.id);
                    n.connections.Add(toAdd);
                }
                count++;
            }
            count = 0;
            foreach(Node node3 in GetUnconnectedNodes(list,list2)){
                IEnumerable<Node> nodesByClose2 = GetNodesSortedByClose(list,node3.realSpace);
                bool flag2 = false;
                foreach(Node node4 in nodesByClose2){
                    if(!DoesPathsIntersect(node3.realSpace, node4.realSpace, nodes)){
                        // Console.WriteLine("At Conn Attempt 2 adding conn"+ node4.id+" "+node3.id);
                        node4.connections.Add(node3);
                        flag2=true;
                        break;
                    }
                }
                if(!flag2){
                    Node toAdd = random.Choice(list);
                    // Console.WriteLine("failsafe 2 adding conn"+toAdd.id+" "+node3.id);
                    toAdd.connections.Add(node3);
                }

                count++;
            }
            
            foreach(Node node5 in list){
                if(random.Range(0f,100f)<25f){
                    foreach(Node node6 in GetNodesSortedByClose(list2, node5.realSpace).Reverse<Node>()){
                        if(!DoesPathsIntersect(node5.realSpace,node6.realSpace, nodes)){
                            // Console.WriteLine("At Conn Attempt 3 adding conn"+ node5.id+" "+node6.id);

                            node5.connections.Add(node6);
                            break;
                        }
                    }
                }
            }
            i2=i;
        }
        Node end = new Node();
        end.depth = depth+1;
        end.nodeType=NodeType.Boss;
        end.id=nodes.Count;
        end.connections=new List<Node>();
        float z = nodes[nodes.Count-1].realSpace.Z+15;
        end.realSpace=new Vector3(0,10,z);

        nodes.Add(end);

        // Console.WriteLine(nodes.Count);

        foreach(Node from2 in nodes.FindAll((x)=> x.depth==depth)){
            from2.connections.Add(end);
        }

        

        MakeShops(depth, ref random,ref nodes);
        // random.Next();\
        
        SetPercentageRandom(NodeType.Challenge,0.07f, nodes, ref random);
        SetPercentageRandom(NodeType.Encounter,0.1f, nodes, ref random);
        SetPercentageRandom(NodeType.RestStop,0.07f, nodes, ref random);
        SetPercentageRandom(NodeType.Shop,0.02f, nodes, ref random);
       


        // foreach(Node n in nodes){
        //     Console.WriteLine(n.nodeType+" "+n.depth+" "+n.id);
        // }
        // foreach(Node n in nodes){
        //     foreach(Node n2 in n.connections){
        //         Console.WriteLine(n.id+" -> "+n2.id);
        //     }
        // }
        // printPaths(nodes[0],"");
        List<List<Node>> paths = new();
        int l = depth+1;
        try{
            getOptimalPaths(nodes[0], 0, ref l, new(), ref paths, minLength);
        }catch(Exception e){
            Console.WriteLine(seed+ " "+e.Message);
        }

        List<PathInfo> validPath = new List<PathInfo>();
        for(int p=0;p<paths.Count;p++){
            if(checkPathForItems(seed, paths[p], items, 4, out List<ItemHitInfo> itemHitInfos)){
                PathInfo pathInfo = new PathInfo(paths[p], itemHitInfos);
                // pathInfo.Path = paths[p];
                // Console.WriteLine(itemHitInfos.Count);
                // foreach(ItemHitInfo itemHitInfo in itemHitInfos){
                //     foreach(int hitInfo in itemHitInfo.items){
                //         // Console.WriteLine(Items.items[hitInfo].name);
                //     }
                // }
                // pathInfo.ItemHitInfos = itemHitInfos;
                validPath.Add(pathInfo);
            }
        }
        // foreach(List<Node> cPath in paths){
        //     Console.WriteLine("Path Length: "+l);
        //     foreach(Node n in cPath){
        //         Console.Write(n.nodeType+" -> ");
        //     }
        //     Console.WriteLine();
        // }
        if(validPath.Count > 0){
            if(l==minLength){
                Console.WriteLine("adding seed:"+seed);
                dict.Add(seed, validPath);
            }else if(l<minLength){
                Console.WriteLine("replacying seed:"+seed);
                dict=new();
                dict.Add(seed, validPath);
            }
            return l;
        }else 
            return depth+1;
    }

    public static bool checkPathForItems(int seed, List<Node> path, List<int> items, int depth, out List<ItemHitInfo> itemHitInfos){
        itemHitInfos = new();
        
        for(float i=.5f; i<=2f;i+=.25f){
            bool isValidPath = true;
            List<int> seedItems = new();
            foreach(Node node in path){
                if(node.nodeType==NodeType.Shop){
                    List<int> j = checkShopItems(seed, node, 2, i);
                    if(j.Count>0){
                        ItemHitInfo itemHitInfo = new ItemHitInfo(node, j, i);
                        itemHitInfos.Add(itemHitInfo);
                        seedItems.AddRange(j);
                    }
                    
                }else if(node.nodeType==NodeType.Encounter){

                }

                if(node.depth>=depth){
                    break;
                }
            }
            if(seedItems.Count>0){
                foreach(int item in items){
                
                    if(!seedItems.Contains(item)){
                        // Console.WriteLine("Couldnt Find: "+item);
                        itemHitInfos = new();
                        isValidPath=false;
                        break;
                    }else{
                        // Console.WriteLine("Found: "+item);
                    }
                }
            }else{
                itemHitInfos = new();
                isValidPath=false;
                    
            }
            if(isValidPath){
                // Console.WriteLine("Return with valid path");
                // Console.WriteLine(itemHitInfos.Count+" "+seedItems.Count);
                return true;

            }
                
        }
        return false;
    }

    public struct PathInfo{
        public List<Node> Path;
        public List<ItemHitInfo> ItemHitInfos;
        public PathInfo(List<Node> Path, List<ItemHitInfo> ItemHitInfos){
            this.Path = Path;
            this.ItemHitInfos = ItemHitInfos;
        }
    }
    public struct ItemHitInfo{
        public NodeType nodeType;
        public int depth;
        public List<int> items;
        public float rarity;
        public ItemHitInfo(Node node, List<int> items, float rarity){
            this.nodeType = node.nodeType;
            this.depth = node.depth;
            this.items = items;
            this.rarity = rarity;
        }
    }
    public static List<int> checkShopItems(int seed, Node node, int rollDepth, float itemRarityModifier){
        List<int> shopItems = new();
        
        List<int> cItems = GetItems(new Random(seed+node.id), itemRarityModifier, node, null);
        shopItems.AddRange(cItems);
        for(int i = 1; i<rollDepth; i++){
            cItems = GetItems(new Random(seed+node.id+i), itemRarityModifier, node, cItems);
            shopItems.AddRange(cItems);
        }

        return shopItems;
    }
    private static List<int> GetItems(Random random, float itemRarityModifier, Node node, List<int> excludedItems=null){
        List<int> list = new List<int>();
        for(int i = 0; i<3; i++){
            List<int> list2 = new List<int>();
            list2.AddRange(list);
            if(excludedItems!=null){
                list2.AddRange(excludedItems);
            }
            int randomItem = GetRandomItem(random, list2, GetShopRarityModifier(node.depth),itemRarityModifier);
            list.Add(randomItem);
        }
        return list;
    }

    private static float GetShopRarityModifier(int depth){
        float num = 0;
        float num2 = MathF.Max(0,MathF.Min(1, (float)GetDifficulty(depth)/20f * 0.3f));
        return (num+num2)* GetRunProgress(depth);
    }

    private static int GetRandomItem(Random random,List<int> excludedItems, float additionalRarity,float itemRarityModifier){
        int item = -1;
        int num = 0;
        while ( num<1000&&item==-1){
            int r2 = GetRandom(random, GetRarityFromPlayerWeight(random, additionalRarity+itemRarityModifier));
            if(excludedItems==null || !excludedItems.Contains(r2)){
                item = r2;
            }
            num++;
        }
        return item;
    }

    public static int GetRandom( Random random, Items.Rarity itemRarity){

       List<Items.Item> list = new();
        foreach(Items.Item item in Items.items){
            if(item.rarity==itemRarity){
                list.Add(item);
            }
        }
        return random.Choice(list).index;
       
    }

    public static Items.Rarity GetRarityFromPlayerWeight(Random random, float additionalRarity){
        float num = additionalRarity/2.75f;
        float num2 = 1f;
        float num3 = num;
        float num4 = num*num;
        float num5 = num*num*num;
        float num6 = num2+num3+num4+num5;
        float num7 = random.NextFloat()*num6;
        if(num7>num2+num3+num4){
            return Items.Rarity.Legendary;
        }
        if (num7 > num2 + num3)
		{
			return Items.Rarity.Epic;
		}
		if (num7 > num2)
		{
			return Items.Rarity.Rare;
		}
		return Items.Rarity.Common;
    }

    public static float GetRunProgress(int depth){
        return depth/ShardDatabase.GetCurrentShardData().depth;
    }

    private static int GetDifficulty(int depth){
        float t = GetRunProgress(depth);
        float num = float.Lerp(ShardDatabase.GetCurrentShardData().minDifficulty, ShardDatabase.GetCurrentShardData().maxDifficulty, t);
        return RoundToInt(num);
    }

    public static void getOptimalPaths(Node node, int frags, ref int minFrags, List<Node> cPath, ref List<List<Node>> paths, int minLength){
        cPath.Add(node);
        if(cPath.Count>100){
            throw new Exception("Erm thats WAY too big");
        }
        // foreach(Node n in cPath){
        //         Console.Write(n.nodeType+" -> ");
        //     }
        if(node.nodeType==NodeType.Boss){
            if(frags<minFrags){
                minFrags=frags;
                paths = new();
                paths.Add(cPath);
            }else if(frags == minFrags){
                paths.Add(cPath);
            }
            return;
        }
        int a = frags;
        if(node.nodeType==NodeType.Default||node.nodeType==NodeType.Challenge){
            a++;
        }
        if(a>minFrags||a>minLength){
            return;
        }
        foreach(Node n in node.connections){
            getOptimalPaths(n,a,ref minFrags, new(cPath), ref paths, minLength);
        }
        
    }

    public static void printPaths(Node node, string str){
        if(node.connections.Count==0){
            Console.WriteLine(str+node.id);
            return;
        }
        foreach(Node con in node.connections){
            string s= str + node.id+" -> ";
            printPaths(con, s);
        }
    }
    public static void SetPercentageRandom(NodeType type, float percentage, List<Node> nodes, ref Random random){
        int num = RoundToInt((float) nodes.Count*percentage);
        for(int i = 0; i<num; i++){
            List<Node> list = nodes.Skip<Node>(1).Where<Node>((Func<Node, bool>)(n=>AllowedToConvertNode(n,type,nodes))).ToList<Node>();
            if(list.Count==0){
                break;
            }
            foreach (Node n in list){
                // Console.WriteLine(n.nodeType+" "+n.id+" is in list");
            }
            if(list.Count==19){
                list.RemoveAt(list.Count-1);
            }
            Node node = random.Choice<Node>(list);
            // Console.WriteLine(node.id+" "+node.nodeType+" became "+type);
            node.nodeType=type;
        }
    }
// float chalC, float encC, float restC, float shopC
    public static void MakeShops(int depth, ref Random random, ref List<Node> nodes){
        // List<int> result = new List<int>();
        for(int i=3; i<depth; i+=4){
            MakeShopsAtDepth(i,ref random,ref nodes);
           
        }
        MakeShopsAtDepth(depth,ref random,ref nodes);
        // return result;
    }
    public static int RoundToInt(float val){
        if(val-(int)val>.5f){
            return (int)val+1;
        }else if(val-(int)val<.5f){
            return (int)val;
        }else if((int) val%2==0){
            return (int)val;
        }else{
            return (int)val+1;
        }
    }

    private static void MakeShopsAtDepth(int depth, ref Random random, ref List<Node> nodes){
        Node[] array = nodes.FindAll((x)=> x.depth==depth).ToArray<Node>();
        // List<int> result = new List<int>();
        
            Node y =  random.Choice<Node>(array);
            foreach(Node n in array){
                // Console.WriteLine("shopping at depth:"+ n.depth);
                if((n.id==y.id|| random.NextFloat()>0.5f)&&AllowedToConvertNode(n,NodeType.Shop, nodes)){
                    n.nodeType=NodeType.Shop;
                    // result.Add(n.id);
                    // Console.WriteLine(n.id+ " Set as shop!");
                }
               
            }
            // return result;
    }
    public static bool AllowedToConvertNode(Node n, NodeType type, List<Node> nodes){
        // if(n.nodeType==NodeType.Default&&!wouldBeDouble(n,type,nodes)&&(type==NodeType.Challenge||wouldBeStreakOf(n,type,nodes)<=2)&&(type!=NodeType.Shop||n.depth>=3)){
        //     Console.WriteLine("Node allowed! "+n.id+ " "+n.nodeType+" can become "+type);
        // }else{
        //     Console.WriteLine("Node not allowed! "+n.id+ " "+n.nodeType+" wants to be "+type);
        // }
        return n.nodeType==NodeType.Default&&
        !wouldBeDouble(n,type,nodes)&&
        (type==NodeType.Challenge||wouldBeStreakOf(n,type,nodes)<=2)&&
        (type!=NodeType.Shop||n.depth>=3);
    }
    
    public static int wouldBeStreakOf(Node node, NodeType type, List<Node> nodes){
        int streak = 1;
        List<Node> depthMinusOne = nodes.FindAll((x)=>x.depth==node.depth-1);
        foreach(Node n in depthMinusOne){
            if(n.nodeType==type&&n.connections.Contains(node)){
                streak = 2;
                List<Node> depthMinusTwo = nodes.FindAll((x)=>x.depth==node.depth-2);
                foreach(Node n2 in depthMinusTwo){
                    if(n2.nodeType==type&&n2.connections.Contains(n)){
                        return 3;
                    }
                }
            }
        }
        foreach(Node n in node.connections){
            if(n.nodeType==type){
                if(streak==2){
                    return 3;
                }
                foreach(Node n2 in n.connections){
                    if(n2.nodeType==type){
                        return 3;
                    }
                }
            }
        }
        return streak;
    }
    
    public static bool wouldBeDouble(Node node, NodeType type, List<Node> nodes){
        foreach (Node n in nodes){
            foreach(Node n2 in n.connections){
                if(n2.id == node.id){
                    if(n.nodeType==type){
                        return true;
                    }
                }
            }
            if(n.id==node.id){
                foreach (Node n3 in n.connections){
                    if(n3.nodeType==type){
                        return true;
                    }
                }
            }
        }
        return false;
    }

    

    static List<Node> GetUnconnectedNodes(List<Node> lower, List<Node> upper){
        List<Node> toReturn = new List<Node>(upper);
        foreach(Node n in lower){
            foreach(Node n2 in n.connections){
                toReturn.RemoveAll((x)=>n2.id==x.id);
            }
        }
        return toReturn;
    }

    static bool IsValidPosition(Vector3 spawnPos, List<Node> nodes)
	{
		using (List<Node>.Enumerator enumerator = nodes.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Vector3.Distance(enumerator.Current.realSpace, spawnPos) < 5f)
				{
					return false;
				}
			}
		}
		return true;
	}

    private static IEnumerable<Node> GetNodesSortedByClose(List<Node> nodes, Vector3 pos)
	{
		return from x in nodes
		orderby Vector3.Distance(pos, x.realSpace)
		select x;
	}

    private static bool DoesPathsIntersect(Vector3 start, Vector3 end, List<Node> nodes)
	{
		foreach (Node node in nodes)
		{
            foreach(Node con in node.connections){
                if (tools.AreLinesIntersecting(con.realSpace.xz(), node.realSpace.xz(), start.xz(), end.xz(), false))
                {
                    return true;
                }
            }
		}
		return false;
	}

}

public static class Items{
    public static Item[] items = {
        new Item(0, "Rocket Boots", Rarity.Common),
        new Item(1, "Energy Lash", Rarity.Epic),
        new Item(2, "Replenishing Vial",Rarity.Common),
        new Item(3, "Mysterious Spring",Rarity.Rare),
        new Item(4, "Standard Redirector", Rarity.Rare),
        new Item(5, "Personal Matter Stabilizer", Rarity.Rare),
        new Item(6, "Time Dilation Thing", Rarity.Epic),
        new Item(7, "Spark Dasher", Rarity.Epic),
        new Item(8, "Blood Engine", Rarity.Legendary),
        new Item(9, "Velocity Powered Syringe",Rarity.Rare),
        new Item(10, "Experimental Autopilot", Rarity.Legendary),
        new Item(11, "Grunts Helmet", Rarity.Rare),
        new Item(12, "Protective Medallion", Rarity.Rare),
        new Item(13, "Impulse Activated Stabilizer", Rarity.Rare),
        new Item(14, "Painful Coil", Rarity.Epic),
        new Item(15, "Well Earned Confidence", Rarity.Rare),
        new Item(16, "BOOSTR POG", Rarity.Epic),
        new Item(17, "Pungent Herbs", Rarity.Epic),
        new Item(18, "Shortcut", Rarity.Common),
        new Item(19, "Tight Schedule", Rarity.Epic),
        new Item(20, "Flashback", Rarity.Legendary),
        new Item(21, "Adrenaline", Rarity.Common),
        new Item(22, "Restorative Maneuver", Rarity.Common),
        new Item(23, "Delayed Emergency Device", Rarity.Rare),
        new Item(24, "N-Dimensional-leaf Clover", Rarity.Rare),
        new Item(25, "Planar Reconfiguration", Rarity.Epic),
        new Item(26, "Atomic Timepiece", Rarity.Epic),
        new Item(27, "General Relativity", Rarity.Epic),
        new Item(28, "Overwound Pocketwatch", Rarity.Epic),
        new Item(29, "Shiny Anchor Pin", Rarity.Rare),
        new Item(30, "Vitamins", Rarity.Rare),
        new Item(31, "Heirs Determination", Rarity.Rare),
        new Item(32, "Perpetual Motion Machine", Rarity.Common),
        new Item(33, "Plutonium Coin", Rarity.Common),
        new Item(34, "Performance Based Health Insurance", Rarity.Rare),
        new Item(35, "Impact Activated Healing Drone", Rarity.Epic),
        new Item(36, "Leadership Pipe", Rarity.Epic),
        new Item(37, "Karma", Rarity.Epic),
        new Item(38, "Brittle Breastplate", Rarity.Epic),
        new Item(39, "Steel Hat Lining", Rarity.Epic),
        new Item(40, "Portable Harvester", Rarity.Legendary),
        new Item(41, "Otherworldly Contract", Rarity.Legendary),
        new Item(42, "Personal Gravity Enhancer", Rarity.Epic),
        new Item(43, "Timeline Shifter", Rarity.Epic),
        new Item(44, "Recyclable Rocket", Rarity.Rare),
        new Item(45, "Emergency Shoes", Rarity.Rare),
        new Item(46, "Fragile Confidence", Rarity.Epic),
        new Item(47, "Dynamo Treadmill", Rarity.Common),
        new Item(48, "Distance-Based Health Insurance", Rarity.Common),
        new Item(49, "Reheated Soup", Rarity.Rare),
        new Item(50, "Intangibility", Rarity.Rare),
        new Item(51, "Greed Machine", Rarity.Rare),
        new Item(52, "Timeline Refractor", Rarity.Epic),
        new Item(53, "Ring Materializer", Rarity.Epic),
        new Item(54, "Fragile Taco", Rarity.Rare),
        new Item(55, "Speedy Recovery", Rarity.Rare),
        new Item(56, "Timeline Refactor", Rarity.Epic),
        new Item(57, "Shimmering Condenser",Rarity.Common),
        new Item(58, "Transition Slingshot", Rarity.Common),
        new Item(59, "Void Charger", Rarity.Common),
        new Item(60, "Pocket Snack", Rarity.Common),
        new Item(61, "Void Compressor", Rarity.Common),
        new Item(62, "Spark Powered Propeller", Rarity.Rare),
        new Item(63, "Spark Furnace", Rarity.Common),
        new Item(64, "Mortar and Pestle", Rarity.Common),
        new Item(65, "Friendly Looking Star", Rarity.Epic),
        new Item(66, "Golden Necklace", Rarity.Common),
        new Item(67, "Secret Technique Instuctions", Rarity.Rare),
        new Item(68, "Overcomplicated Coin", Rarity.Legendary),
        new Item(69, "Overclocked Medical Drone", Rarity.Rare),
        new Item(70, "Clown Shoes", Rarity.Rare),
        new Item(71, "Aromatic Herbs", Rarity.Common),
        new Item(72, "Low Grade Timeline Swapper", Rarity.Epic),
        new Item(73, "Bitter Herbs", Rarity.Rare),
        new Item(74, "Extreme Herbs", Rarity.Epic),
        new Item(75, "Momentum Recalibrator", Rarity.Rare),
        new Item(76, "Jackpot", Rarity.Rare),
        new Item(77, "High Risk Investment", Rarity.Rare),
        new Item(78, "Steady Investment", Rarity.Rare),
        new Item(79, "Interest", Rarity.Rare),
        new Item(80, "Dangerous Investment Scheme", Rarity.Legendary),
        new Item(81, "Big Spark Magnet", Rarity.Rare),
        new Item(82, "400-leaf Clover", Rarity.Rare),
        new Item(83, "Heart Shaped Mirror", Rarity.Rare),
        new Item(84, "Big Pumpkin", Rarity.Common),
        new Item(85, "Big Squash", Rarity.Rare),
        new Item(86, "Quick Taco", Rarity.Epic),
        new Item(87, "Growth Potential", Rarity.Rare),
        new Item(88, "Instant Compensation Machine", Rarity.Epic),
        new Item(89, "Experimental Thrusters", Rarity.Legendary),
        new Item(90, "Wingspan", Rarity.Legendary)
        
    };
   
    public struct Item{
        public int index;
        public string name;
        public Rarity rarity;
        public Item(int index, string name, Rarity rarity){
            this.index = index;
            this.name = name;
            this.rarity = rarity;
        }
    }
    public enum Rarity{
        Common,
        Rare,
        Epic,
        Legendary
    }
}


public static class tools{
    public static float NextFloat(this System.Random random)
	{
		return (float)random.NextDouble();
	}
    public static float Range(this System.Random random, float min, float max)
	{
		return (float)random.NextDouble() * (max - min) + min;
	}
    public static T Choice<T>(this System.Random random, T[] array)
	{
		return array[random.Next(array.Length)];
	}
    public static T Choice<T>(this System.Random random, List<T> array)
	{
		return array[random.Next(array.Count)];
	}

    public static bool AreLinesIntersecting(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2, bool shouldIncludeEndPoints)
		{
			float num = 1E-05f;
			bool result = false;
			float num2 = (l2_p2.Y - l2_p1.Y) * (l1_p2.X - l1_p1.X) - (l2_p2.X - l2_p1.X) * (l1_p2.Y - l1_p1.Y);
			if (num2 != 0f)
			{
				float num3 = ((l2_p2.X - l2_p1.X) * (l1_p1.Y - l2_p1.Y) - (l2_p2.Y - l2_p1.Y) * (l1_p1.X - l2_p1.X)) / num2;
				float num4 = ((l1_p2.X - l1_p1.X) * (l1_p1.Y - l2_p1.Y) - (l1_p2.Y - l1_p1.Y) * (l1_p1.X - l2_p1.X)) / num2;
				if (shouldIncludeEndPoints)
				{
					if (num3 >= 0f + num && num3 <= 1f - num && num4 >= 0f + num && num4 <= 1f - num)
					{
						result = true;
					}
				}
				else if (num3 > 0f + num && num3 < 1f - num && num4 > 0f + num && num4 < 1f - num)
				{
					result = true;
				}
			}
			return result;
		}
   public static Vector2 xz(this Vector3 me)
		{
			return new Vector2(me.X, me.Z);
		}

        // public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
		// {
			
		// 	return new Enumerable.ReverseIterator<TSource>(source);
		// }

    
}



public static class ShardDatabase{
    static int currentShard = 1;
    static shardData[] shardDatas= {
        new Shard1Data(),
        new Shard2Data(),
        new Shard3Data(),
        new Shard4Data(),
        new Shard5Data(),
        new Shard6Data(),
        new Shard7Data(),
        new Shard8Data(),
        new Shard9Data(),
        new Shard10Data()
    };
    public static shardData GetCurrentShardData(int id=-1){
        if(id!=-1){
            currentShard = id;
        }
        return shardDatas[currentShard-1];
    }


}

public class Shard1Data : shardData{
    public Shard1Data(){
        minDifficulty=0;
        maxDifficulty=6;
        depth=12;
    }
}
public class Shard2Data : shardData{
    public Shard2Data(){
        minDifficulty=4;
        maxDifficulty=9;
        depth=13;
    }

}
public class Shard3Data : shardData{
    public Shard3Data(){
        minDifficulty=8;
        maxDifficulty=13;
        depth=14;
    }
}
public class Shard4Data : shardData{
    public Shard4Data(){
        minDifficulty=9;
        maxDifficulty=16;
        depth=15;
    }

}
public class Shard5Data : shardData{
    public Shard5Data(){
        minDifficulty=10;
        maxDifficulty=18;
        depth=16;
    }

}
public class Shard6Data : shardData{
    public Shard6Data(){
        minDifficulty=12;
        maxDifficulty=20;
        depth=17;
    }
}
public class Shard7Data : shardData{
    public Shard7Data(){
        minDifficulty=14;
        maxDifficulty=22;
        depth=18;
    }
}
public class Shard8Data : shardData{
    public Shard8Data(){
        minDifficulty=16;
        maxDifficulty=24;
        depth=19;
    }

}
public class Shard9Data : shardData{
    public Shard9Data(){
        minDifficulty=20;
        maxDifficulty=28;
        depth=20;
    }
    
}
public class Shard10Data : shardData{
    public Shard10Data(){
        minDifficulty=22;
        maxDifficulty=30;
        depth=25;
    }
    
}


public class shardData{
    public int depth;
    public int minDifficulty;
    public int maxDifficulty;

}
