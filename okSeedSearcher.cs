using System.Numerics;

int shard = 1;
SearchClass.globalMin=ShardDatabase.GetCurrentShardData(shard).depth+1;

ItemInstanceOption[] ItemSearch = new ItemInstanceOption[] {4, 27, new([27,28]), new([16, 0, 32, 29])};
ItemSearchParams searchParams = new ItemSearchParams(4,ItemSearch,2);

int searchStart = 0;
int searchEnd = 10000000;



Dictionary<int, (List<SE_Node>,ItemRarityResults)> ValidSeeds = new();

for(int i = searchStart; i<searchEnd; i++){
    SE_Info info = new SE_Info();
    info.shard = ShardDatabase.GetCurrentShardData().shard;
    info.seed = i;

    if(i%5000==0){
        Console.WriteLine((i-searchStart)+"/"+(searchEnd-searchStart)+" "+(float)(i-searchStart)/(searchEnd-searchStart)+"% Seeds searched. Found "+ValidSeeds.Keys.Count+" seeds with length "+SearchClass.globalMin);
    }

    GenerationClass.GenerateSimplified(ShardDatabase.GetCurrentShardData().depth, new Random(info.seed), info);

    
    ItemSearchResults results = new();

    
    SearchClass.FindItems(info, searchParams, results);
    if(results.FoundItems){
        List<SE_Node> path = SearchClass.FindPathWithItems(info,searchParams, results);
        if(path!=null){
            int length = CountNodeLength(path);
            if(SearchClass.globalMin>length){
                SearchClass.globalMin = length;
                ValidSeeds = new();
                ValidSeeds.Add(i, (path,results.winningRarityResults));
                Console.WriteLine("New min node found! Length: "+length+". replacing dict. Seed: "+i);
            }else{
                ValidSeeds.Add(i, (path,results.winningRarityResults));
                Console.WriteLine("Adding seed to dict. Seed: "+i);
            }
        }
    }


    
}

foreach(int key in ValidSeeds.Keys){
    Console.WriteLine("Current Seed:"+key);
    foreach(SE_Node node in ValidSeeds[key].Item1){
        Console.WriteLine("\t"+node);
    }
    Console.WriteLine("\t\tItem info: "+ValidSeeds[key].Item2);
}



static int CountNodeLength(List<SE_Node> path){
    int length = 0;
    foreach(SE_Node node in path){
        if(node.Type==NodeType.Default||node.Type==NodeType.Challenge){
            length++;
        }
    }
    return length;
}


// foreach(SE_Node node in info.nodes){
//     Console.WriteLine(node);
// }

// foreach(SE_Path path in info.paths){
//     Console.WriteLine(path);
// }


public enum NodeType{
    Default,
    Challenge,
    Shop,
    Encounter,
    RestStop,
    Boss
}

public class SE_Node {
    public int ID;
    public int Depth;
    public Vector3 Position;
    public NodeType Type;
    public List<SE_Node> pathsTo=new();
    public string Notes = string.Empty;

    public override string ToString() {
        return $"ID: {ID}, Depth: {Depth}, Type: {Type}, Position: {Position}";
    }
}


public class SE_Path {
    public int FromNodeID;
    public int ToNodeID;

    public override string ToString() {
        return $"From: {FromNodeID}, To: {ToNodeID}";
    }
}

[Serializable]
public class SE_Info {
    
    public List<SE_Node> nodes = new();
    
    public List<SE_Path> paths = new();

    public int shard;
    public int seed;

    public override string ToString() {
        return string.Join('\n',
            $"Nodes: {nodes.Count}, Paths: {paths.Count}",
            string.Join('\n', nodes.Select(n => n.ToString())),
            string.Join('\n', paths.Select(p => p.ToString())));
    }
}

public class SE_RunPathID : List<int>
{
    public SE_RunPathID(IEnumerable<int> collection) : base(collection)
    {
    }

    
}

public class SE_RunPathType : List<NodeType> { }

public class ItemSearchParams{
    public int MaxDepth;
    public int MaxShopRolls;
    public ItemInstanceOption[] itemInstanceOptions;

    public ItemSearchParams(int maxDepth, ItemInstanceOption[] itemInstanceOptions, int maxShopRolls){
        this.MaxDepth = maxDepth;
        this.itemInstanceOptions = itemInstanceOptions;
        this.MaxShopRolls = maxShopRolls;
    }
    public bool EvaluateItems(List<int> items, out List<int> validItems){
        validItems = new List<int>();
        if(itemInstanceOptions.Length==0){
            validItems=items;
            return true;
        }
        foreach(ItemInstanceOption cOption in itemInstanceOptions){
            foreach(int i in items){
                if(cOption.items.Contains(i)){
                    validItems.Add(i);
                }
            }
        }
        return validItems.Count > 0;
    }
}

public class ItemInstanceOption{
    public List<int> items = new();
    public List<NodeType> nodeTypes= new(){NodeType.Shop, NodeType.Challenge};
    public ItemInstanceOption(int[] ints, List<NodeType> targetNodeTypes=null){
        items.AddRange(ints);
        if(targetNodeTypes!=null){
            nodeTypes=targetNodeTypes;
        }
    }

    public bool matches(ItemResultHit hit){
        if(!nodeTypes.Contains(hit.node.Type)){
            return false;
        }

        if(items.Contains(hit.item)){
            return true;
        }
        return false;
    }

    public static implicit operator ItemInstanceOption(int x){
        return new ItemInstanceOption([x]);
    }
}


public class ItemSearchResults{
    public List<ItemRarityResults> itemRarityResults;
    public ItemRarityResults winningRarityResults;
    public bool FoundItems= false;

    public ItemSearchResults(){
        itemRarityResults = new List<ItemRarityResults>();
        for(float i = 0.25f; i<=2; i+=.25f){
            itemRarityResults.Add(new ItemRarityResults(i));
        }
    }
    public override string ToString(){
        string toReturn = "";
        foreach(ItemRarityResults cRarity in itemRarityResults){
            toReturn += cRarity.ToString()+"\n";
        }
        return toReturn;
    }
}


public class ItemRarityResults{
    public float itemRarity;
    public List<ItemResultHit> itemHits=new();
    public ItemRarityResults(float i){itemRarity=i;}
    public override string ToString(){
        string toReturn="Current item rarity: "+itemRarity+"\n";
        foreach(ItemResultHit hit in itemHits){
            toReturn += "\tItem: "+Items.items[hit.item].name+" Found in: "+hit.node.Type+" At depth: "+hit.node.Depth+" Position: "+hit.node.Position+"\n";
        }
        return toReturn;
    }
}

public class ItemResultHit{
    public SE_Node node;
    public int item;
    public int roll=-1;

}


public static class SearchClass{
    public static int globalMin;
    public static void FindItems(SE_Info info, ItemSearchParams Params, ItemSearchResults results){
        for(int i = 0; i < results.itemRarityResults.Count; i++){
            FindItemsWithRarity(results.itemRarityResults[i], Params, info, results);
        }
    }

    public static List<SE_Node> FindPathWithItems(SE_Info info, ItemSearchParams Params, ItemSearchResults results){
        List<List<SE_Node>> paths = new();
        int i = ShardDatabase.GetCurrentShardData().depth;
        
        FindMinPaths(info.nodes.First(), 0, ref i, new(), ref paths);
        foreach(List<SE_Node> path in paths){
            if(IdkWhatToCallThisFunctionBecauseIAmOnceAgainCodingAtFiveAMAndCannotThinkProperlyButItBasicallyChecksIfAPathContainsSatisfactoryItemHits(path, Params, results)){
                return path;
            }
        }
        return null;

    }

    static bool IdkWhatToCallThisFunctionBecauseIAmOnceAgainCodingAtFiveAMAndCannotThinkProperlyButItBasicallyChecksIfAPathContainsSatisfactoryItemHits(List<SE_Node> path, ItemSearchParams Params, ItemSearchResults results){
        foreach(ItemRarityResults rarityResults in results.itemRarityResults){
            List<ItemInstanceOption> options = new(Params.itemInstanceOptions);
            foreach(SE_Node node in path.Where<SE_Node>((x)=>x.Depth<=Params.MaxDepth)){
                foreach(ItemResultHit hit in rarityResults.itemHits.FindAll((x)=> x.node.ID==node.ID)){
                    for(int i=0; i<options.Count;){
                        if(options[i].matches(hit)){
                            options.RemoveAt(i);

                        }else{

                            i++;
                        }
                    }
                }
            }
            if(options.Count==0){
                results.winningRarityResults=rarityResults;
                return true;
            }
        }
        

        return false;
    }

    public static void FindMinPaths(SE_Node node, int frags, ref int localMin, List<SE_Node> cPath, ref List<List<SE_Node>> paths){
        cPath.Add(node);
        if(node.Type==NodeType.Boss){
            if(frags<globalMin){
                localMin=frags;
                paths = new();
                paths.Add(cPath);
            }else if(frags == localMin){
                paths.Add(cPath);
            }
            return;
        }
        int a = frags;
        if(node.Type==NodeType.Default||node.Type==NodeType.Challenge){
            a++;
            if(a>localMin||a>globalMin){
                return;
            }
        }
        foreach(SE_Node n in node.pathsTo){
            FindMinPaths(n,a, ref localMin, new(cPath), ref paths);
        }
        
    }

    private static void FindItemsWithRarity(ItemRarityResults itemRarityResults, ItemSearchParams Params, SE_Info info,ItemSearchResults results){
        List<SE_Node> nodes = info.nodes.FindAll((x)=> x.Depth<=Params.MaxDepth);

        HashSet<NodeType> typesToInvestigate = new();
        foreach(ItemInstanceOption itemInstanceOption in Params.itemInstanceOptions){
            foreach(NodeType n in itemInstanceOption.nodeTypes){
                typesToInvestigate.Add(n);
            }
        }
        if(typesToInvestigate.Count==0){
            typesToInvestigate.Add(NodeType.Encounter);
            typesToInvestigate.Add(NodeType.Shop);
            typesToInvestigate.Add(NodeType.Challenge);
        }


        foreach(SE_Node node in nodes){

            if(typesToInvestigate.Contains(node.Type)){
                investigateNode(node, Params, itemRarityResults, info, results);
            }
        }
    }

    private static void investigateNode(SE_Node node, ItemSearchParams Params, ItemRarityResults itemRarityResults, SE_Info info, ItemSearchResults results){
        switch(node.Type){
            case NodeType.Shop:
                // List<int> shopItems = new();
            
                List<int> cItems = GetItems(new Random(info.seed+node.ID), itemRarityResults.itemRarity, node, null);
                
                if(Params.EvaluateItems(cItems, out List<int> items)){
                    foreach(int i in items){
                        ItemResultHit itemResultHit = new();
                        itemResultHit.item = i;
                        itemResultHit.node = node;
                        itemRarityResults.itemHits.Add(itemResultHit);
                        results.FoundItems=true;
                    }
                }
                for(int i = 1; i<Params.MaxShopRolls; i++){
                    cItems = GetItems(new Random(info.seed+node.ID), itemRarityResults.itemRarity, node, null);
                    if(Params.EvaluateItems(cItems, out List<int> items2)){
                        foreach(int i2 in items2){
                            ItemResultHit itemResultHit = new();
                            itemResultHit.item = i2;
                            itemResultHit.node = node;
                            itemResultHit.roll=i;
                            itemRarityResults.itemHits.Add(itemResultHit);
                            results.FoundItems=true;
                        }
                    }
                }

                break;
            default:
                break;

        }
    }

    private static List<int> GetItems(Random random, float itemRarityModifier, SE_Node node, List<int> excludedItems=null){
        List<int> list = new List<int>();
        for(int i = 0; i<3; i++){
            List<int> list2 = new List<int>();
            list2.AddRange(list);
            if(excludedItems!=null){
                list2.AddRange(excludedItems);
            }
            int randomItem = GetRandomItem(random, list2, GetShopRarityModifier(node.Depth), itemRarityModifier);
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

    private static int GetRandom( Random random, Items.Rarity itemRarity){

       List<Items.Item> list = new();
        foreach(Items.Item item in Items.items){
            if(item.rarity==itemRarity){
                list.Add(item);
            }
        }
        return random.Choice(list).index;
       
    }

    private static Items.Rarity GetRarityFromPlayerWeight(Random random, float additionalRarity){
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

    private static float GetRunProgress(int depth){
        return depth/ShardDatabase.GetCurrentShardData().depth;
    }

    private static int GetDifficulty(int depth){
        float t = GetRunProgress(depth);
        float num = float.Lerp(ShardDatabase.GetCurrentShardData().minDifficulty, ShardDatabase.GetCurrentShardData().maxDifficulty, t);
        return tools.RoundToInt(num);
    }
}

public static class GenerationClass{

    public static void GenerateSimplified(
        int depth,
        Random random,
        SE_Info infos
    ) {
        List<SE_Node> nodes = infos.nodes;
        List<SE_Path> paths = infos.paths;

        // Generate nodes 
        SpawnNode(new Vector3(0, 1, 0) * 10f, 0, nodes, NodeType.Default, "Start node");
        for (int depth1 = 1; depth1 <= depth; ++depth1) {
            int num = random.Next(2, 4);
            for (int index1 = 0; index1 < num; ++index1) {
                for (int index2 = 0; index2 < 15; ++index2) {
                    Vector3 vector3 = new Vector3(random.Range(-20f, 20f), 10f, random.Range(0.0f, 2f) + depth1 * 7f);
                    if (IsValidPosition(vector3, nodes)) {
                        SpawnNode(vector3, depth1, nodes, NodeType.Default, "Init node");
                        break;
                    }
                }
            }
        }

        // Generate paths
        SE_Node from1 = nodes.First();
        foreach (SE_Node to in nodes.FindAll(x => x.Depth == 1)){
            paths.Add(new SE_Path { FromNodeID = from1.ID, ToNodeID = to.ID });
            from1.pathsTo.Add(to);
        }

        for (int i = 1; i < depth; i++) {
            List<SE_Node> all1 = nodes.FindAll(x => x.Depth == i);
            List<SE_Node> all2 = nodes.FindAll(x => x.Depth == i + 1);
            foreach (SE_Node from2 in all1) {
                IEnumerable<SE_Node> nodesSortedByClose = GetNodesSortedByClose(all2, from2.Position);
                bool flag = false;
                foreach (SE_Node to in nodesSortedByClose) {
                    if (!DoesPathsIntersect(from2.Position, to.Position, paths, nodes)) {
                        paths.Add(new SE_Path { FromNodeID = from2.ID, ToNodeID = to.ID });
                        from2.pathsTo.Add(to);
                        flag = true;
                        break;
                    }
                }
                if (!flag) {
                    SE_Node randomNode = random.Choice(all2);
                    paths.Add(new SE_Path { FromNodeID = from2.ID, ToNodeID = randomNode.ID });
                    from2.pathsTo.Add(randomNode);
                }
            }

            foreach (SE_Node unconnectedNode in GetUnconnectedNodes(all2, paths)) {
                IEnumerable<SE_Node> nodesSortedByClose = GetNodesSortedByClose(all1, unconnectedNode.Position);
                bool flag = false;
                foreach (SE_Node from3 in nodesSortedByClose) {
                    if (!DoesPathsIntersect(from3.Position, unconnectedNode.Position, paths, nodes)) {
                        paths.Add(new SE_Path { FromNodeID = from3.ID, ToNodeID = unconnectedNode.ID });
                        from3.pathsTo.Add(unconnectedNode);
                        flag = true;
                        break;
                    }
                }
                if (!flag) {
                    SE_Node randomNode = random.Choice(all1);
                    paths.Add(new SE_Path { FromNodeID = randomNode.ID, ToNodeID = unconnectedNode.ID });
                    randomNode.pathsTo.Add(unconnectedNode);
                }
            }

            foreach (SE_Node from4 in all1) {
                if (random.NextFloat() < 0.25) {
                    foreach (SE_Node to in GetNodesSortedByClose(all2, from4.Position).Reverse()) {
                        if (!DoesPathsIntersect(from4.Position, to.Position, paths, nodes)) {
                            paths.Add(new SE_Path { FromNodeID = from4.ID, ToNodeID = to.ID });
                            from4.pathsTo.Add(to);
                            break;
                        }
                    }
                }
            }
        }

        // Add boss node
        SE_Node to1 = SpawnNode(new Vector3(0.0f, 10f, nodes.Last().Position.Z + 15f), depth + 1, nodes, NodeType.Boss, "Boss node");
        foreach (SE_Node from5 in nodes.FindAll(x => x.Depth == depth)){
            paths.Add(new SE_Path { FromNodeID = from5.ID, ToNodeID = to1.ID });
            from5.pathsTo.Add(to1);
        }


        // Assign node types
        MakeShops(nodes, paths, random, depth);
        SetPercentageRandomToType(NodeType.Challenge, 0.07f);
        SetPercentageRandomToType(NodeType.Encounter, 0.1f);
        SetPercentageRandomToType(NodeType.RestStop, 0.07f);
        SetPercentageRandomToType(NodeType.Shop, 0.02f);


        void SetPercentageRandomToType(
            NodeType type,
            float percentage
        ) {
            int num = tools.RoundToInt(nodes.Count * percentage);
            for (int index = 0; index < num; ++index) {
                List<SE_Node> validNodes = nodes
                    .Skip(1)
                    .Where(n => AllowedToConvertNode(n, type, nodes, paths))
                    .ToList();

                if (validNodes.Count == 0)
                    break;

                SE_Node selectedNode = random.Choice(validNodes);
                selectedNode.Type = type;
                selectedNode.Notes = $"Generated in SetPercentageRandomToType({type}) (p: {percentage}, i: {index})";
            }
        }
    }

    private static void MakeShops(
        List<SE_Node> nodes,
        List<SE_Path> paths,
        Random random,
        int depth
    ) {
        for (int i = 3; i < depth; i += 4)
            SetShopAtDepth(i);
        SetShopAtDepth(depth);

        void SetShopAtDepth(int d) {
            SE_Node[] array = nodes.Where(n => n.Depth == d).ToArray();
            if (array.Length == 0)
                return;
            SE_Node levelSelectionNode = random.Choice(array);
            foreach (SE_Node node in array) {
                if ((node == levelSelectionNode || random.NextFloat() > 0.5) && AllowedToConvertNode(node, NodeType.Shop, nodes, paths)) {
                    node.Type = NodeType.Shop;
                    node.Notes = $"Generated in MakeShops ({d})";
                }
            }
        }
    }

    private static SE_Node SpawnNode(
        Vector3 pos,
        int depth,
        List<SE_Node> nodes,
        NodeType type,
        string notes = ""
    ) {
        SE_Node node = new SE_Node {
            ID = nodes.Count,
            Depth = depth,
            Position = pos,
            Type = type,
            Notes = notes
        };
        nodes.Add(node);
        return node;
    }

    private static bool IsValidPosition(Vector3 spawnPos, List<SE_Node> nodes) {
        foreach (SE_Node node in nodes) {
            if (Vector3.Distance(node.Position, spawnPos) < 5.0)
                return false;
        }
        return true;
    }

    private static List<SE_Node> GetUnconnectedNodes(
        List<SE_Node> nodes,
        List<SE_Path> paths
    ) {
        return nodes.FindAll(x => paths.All(y => y.FromNodeID != x.ID && y.ToNodeID != x.ID));
    }

    private static IEnumerable<SE_Node> GetNodesSortedByClose(
        List<SE_Node> nodes,
        Vector3 pos
    ) {
        return nodes.OrderBy(x => Vector3.Distance(pos, x.Position));
    }

    private static bool DoesPathsIntersect(
        Vector3 start,
        Vector3 end,
        List<SE_Path> paths,
        List<SE_Node> nodes
    ) {
        foreach (SE_Path path in paths) {
            Vector3 pathStart = nodes[path.FromNodeID].Position;
            Vector3 pathEnd = nodes[path.ToNodeID].Position;
            if (tools.AreLinesIntersecting(pathStart.xz(), pathEnd.xz(), start.xz(), end.xz(), false))
                return true;
        }
        return false;
    }

    private static bool AllowedToConvertNode(
        SE_Node node,
        NodeType type,
        List<SE_Node> nodes,
        List<SE_Path> paths
    ) {
        return node.Type == NodeType.Default &&
               !WouldConvertingProduceDoubleNode(node, type, nodes, paths) &&
               (type == NodeType.Challenge || ConvertingWouldProduceStreakOf(node, nodes, paths) <= 2) &&
               (type != NodeType.Shop || node.Depth >= 3);
    }

    private static bool WouldConvertingProduceDoubleNode(
        SE_Node node,
        NodeType type,
        List<SE_Node> nodes,
        List<SE_Path> paths
    ) {
        foreach (SE_Path path in paths) {
            SE_Node? connectedNode = 
                path.FromNodeID == node.ID ? nodes[path.ToNodeID] : 
                path.ToNodeID == node.ID ? nodes[path.FromNodeID] : null;

            if (connectedNode != null && connectedNode.Type == type)
                return true;
        }
        return false;
    }

    private static int ConvertingWouldProduceStreakOf(
        SE_Node node,
        List<SE_Node> nodes,
        List<SE_Path> paths
    ) {
        return Count(node, true) + Count(node, false) + 1;

        int Count(SE_Node n, bool forwards) {
            int maxStreak = 0;
            foreach (SE_Path path in paths) {
                if ((forwards ? path.FromNodeID : path.ToNodeID) != n.ID)
                    continue;
                int otherNodeID = forwards ? path.ToNodeID : path.FromNodeID;
                switch (nodes[otherNodeID].Type) {
                    case NodeType.Shop:
                    case NodeType.Encounter:
                    case NodeType.RestStop:
                        maxStreak = Math.Max(maxStreak, Count(nodes[otherNodeID], forwards) + 1);
                        break;
                }
            }
            return maxStreak;
        }
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



    public class Shard1Data : shardData{
        public Shard1Data(){
            shard=1;
            minDifficulty=0;
            maxDifficulty=6;
            depth=12;
        }
    }
    public class Shard2Data : shardData{
        public Shard2Data(){
            shard=2;
            minDifficulty=4;
            maxDifficulty=9;
            depth=13;
        }

    }
    public class Shard3Data : shardData{
        public Shard3Data(){
            shard=3;
            minDifficulty=8;
            maxDifficulty=13;
            depth=14;
        }
    }
    public class Shard4Data : shardData{
        public Shard4Data(){
            shard=4;
            minDifficulty=9;
            maxDifficulty=16;
            depth=15;
        }

    }
    public class Shard5Data : shardData{
        public Shard5Data(){
            shard=5;
            minDifficulty=10;
            maxDifficulty=18;
            depth=16;
        }

    }
    public class Shard6Data : shardData{
        public Shard6Data(){
            shard=6;
            minDifficulty=12;
            maxDifficulty=20;
            depth=17;
        }
    }
    public class Shard7Data : shardData{
        public Shard7Data(){
            shard=7;
            minDifficulty=14;
            maxDifficulty=22;
            depth=18;
        }
    }
    public class Shard8Data : shardData{
        public Shard8Data(){
            shard=8;
            minDifficulty=16;
            maxDifficulty=24;
            depth=19;
        }

    }
    public class Shard9Data : shardData{
        public Shard9Data(){
            shard=9;
            minDifficulty=20;
            maxDifficulty=28;
            depth=20;
        }
        
    }
    public class Shard10Data : shardData{
        public Shard10Data(){
            shard=10;
            minDifficulty=22;
            maxDifficulty=30;
            depth=25;
        }
        
    }


    public class shardData{
        public int shard;
        public int depth;
        public int minDifficulty;
        public int maxDifficulty;

    }


}

