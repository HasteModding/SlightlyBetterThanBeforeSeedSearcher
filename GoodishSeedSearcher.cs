using System.Numerics;



public static class SeedShenanigans
{
    public static volatile bool report=false;
    public static void Main(string[] args)
    {
        Console.WriteLine("Seed Explorer/Finder Console App");
        Console.WriteLine("Version: 1.0.0 (you know im never gonna update this number)");
        Console.WriteLine("Credits: Fishy & Qwarks");
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("This is a console application for finding seeds in the game.");
        Console.WriteLine("It allows you to generate random seeds, and find specific seeds based on criteria.");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
        Console.Clear();
        Console.WriteLine("What shard would you like to work with? (1-10)");
        int shard = int.Parse(Console.ReadLine());
        Console.WriteLine("How many CPU threads would you like to use? (If you are unsure its recommended to use a value around 20 - most CPUs likely won't benefit from much more than this) (ok being honest I dont totally know myself I dont usually do multithread stuff)");
        int numThreads = int.Parse(Console.ReadLine());
        Console.WriteLine("What seed would you like to start searching from?");
        int seedStart = int.Parse(Console.ReadLine());
        Console.WriteLine("What seed would you like to end searching on?");
        int seedEnd = int.Parse(Console.ReadLine());
        Console.WriteLine("What node depth would you like to cap item searches to? (this is used to ensure that the items we want are found early on in the shard if we want)");
        int searchDepth = int.Parse(Console.ReadLine());
        Console.WriteLine("Minimum path length youd like to search for (for best results use a slight over estimate - this is just used to increase search speed performance) (path length is defined by the number of playable nodes like default nodes and challenge on the path)");
        int pathLength = int.Parse(Console.ReadLine());
        Console.WriteLine("What item search mode would you like to use? (!help)\n1 - Specific\n2 - Total");
        bool useSpecific = false;
        for(;;){
            string response = Console.ReadLine().ToLower();
            if (response == "!help"){
                Console.WriteLine("1 - Specific: Choose a specific node type to find each item in (currently not implemented)");
                Console.WriteLine("2 - Total: Choose node types you want all item nodes to total up to (ie. if you want to find 4 items 2 in shops and 2 in challenges you could enter: Shop, Shop, Challenge, Challenge)");
            }else if(response.Contains("1") ||response.Contains("specific")){
                useSpecific=true;
                break;
            }else if(response.Contains("2")||response.Contains("total")){
                break;
            }
            Console.WriteLine("Please enter a value");
        }
        List<Items.Item> items = new List<Items.Item>();
        List<NodeType[]> nodes = new List<NodeType[]>();
        if(useSpecific){
            Console.WriteLine("Prithee, pardon this vexation, for the feature is as yet unready and doth lie dormant.");
            Console.ReadKey(true);
            return;
        }else{
            Console.WriteLine("Please enter the items you wish for, one at a time, you can enter either the items legal name, or by its item ID (!Items - for a list of all items)\nYou may type !Stop to stop entering items");
            for(;;){
                string response = Console.ReadLine().ToLower();
                // Console.WriteLine(response);
                if(response == "!items"){
                    Console.Clear();
                    printItems();
                    Console.WriteLine("Please enter the items you wish for, one at a time, you can enter either the items legal name, or by its item ID (!Items - for a list of all items)\nYou may type !Stop to stop entering items");
                }else if(response == "!stop"||response=="stop"){
                    break;
                }else{
                    if(Items.Find(response, out Items.Item item)){
                        items.Add(item);
                        Console.WriteLine("Added item: "+item.name);
                    }else{
                        Console.WriteLine("Could not find item or command!");
                    }
                }
            }
            Console.WriteLine("Listed items: ");
            foreach(Items.Item item in items){
                Console.WriteLine(item.index+": "+item.name);
            }
            Console.WriteLine("Please enter the node types you wish find items in one at a time, note the total will equal the length of the item list if you wish to search for a node option you can include multiple types in one line separated by spaces (i.e to add encounter OR challenge to the search (so a free item) you would enter '3 2') (!Nodes - for a list of node types and ID's)");
            while(nodes.Count!=items.Count){
                string response = Console.ReadLine().ToLower();
                string[] respSplit = response.Split(" ");
                NodeType[] nodeOptions = new NodeType[respSplit.Length];
                for(int i=0; i<respSplit.Length;i++){
                    if(respSplit[i] == "!nodes"){
                        Console.WriteLine("1 - Shop");
                        Console.WriteLine("2 - Challenge");
                        Console.WriteLine("3 - Encounter");
                        continue;
                    }else if(respSplit[i]=="1"||respSplit[i]=="shop"){
                        nodeOptions[i]=NodeType.Shop;
                        Console.WriteLine("Added shop");
                    }else if(respSplit[i]=="2"||respSplit[i]=="challenge"){
                        nodeOptions[i]=NodeType.Challenge;
                        Console.WriteLine("Added challenge");
                    }else if(respSplit[i]=="3"||respSplit[i]=="encounter"){
                        nodeOptions[i]=NodeType.Encounter;
                        Console.WriteLine("Added encounter");
                    }else{
                        Console.WriteLine("Could not find NodeType or command!");
                        continue;
                    }
                }
                nodes.Add(nodeOptions);
            }
            Console.WriteLine("Listed nodes: ");
            foreach(NodeType[] node in nodes){
                foreach(NodeType n in node){
                    Console.Write(n+" ");
                }
                Console.WriteLine();
            }
        }

        Console.WriteLine("Press any key to start searching");
        Console.ReadKey(true);

        List<Dictionary<int, (List<SE_Node>, ItemRarityResults)>> threadDicts = new();
        List<int> minNodes = new();
        List<int> threadProgress = new();
        int min = pathLength;
        for(int i=0;i<numThreads;i++){
            threadDicts.Add(new());
        
            minNodes.Add(min);

            threadProgress.Add(0);
        }
        

        
        SearchClass.targetItems = items;
        SearchClass.targetNodes = nodes;
        SearchClass.isSpecific = useSpecific;
        SearchClass.searchDepth = searchDepth;

        long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();


                Parallel.For(0,numThreads,(x)=>{
                    for(int i =seedStart+(seedEnd-seedStart)/numThreads*x; i<seedStart+(seedEnd-seedStart)/numThreads*(x+1);i++){
                        if(threadProgress[x]%2000==0){
                            TimeSpan t = TimeSpan.FromMilliseconds((DateTimeOffset.Now.ToUnixTimeMilliseconds() - start));
                            string answer = string.Format("{4:D1}d:{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", t.Hours, t.Minutes, t.Seconds, t.Milliseconds, t.Days);
                            Console.WriteLine("Thread "+x+" has searched "+threadProgress[x]+" seeds. Started on: "+(seedStart+(seedEnd-seedStart)/numThreads*x)+" Progress: "+(float)threadProgress[x]/((seedEnd-seedStart)/numThreads)*100+"% Found: "+threadDicts[x].Keys.Count+" Seeds with min nodes of: "+minNodes[x]+" Time: "+answer);
                        }
                        threadProgress[x]++;
                        SE_Info info = new SE_Info();
                        info.shard = ShardDatabase.GetCurrentShardData().shard;
                        info.seed = i;

                        GenerationClass.GenerateSimplified(ShardDatabase.GetCurrentShardData().depth, new Random(info.seed), info);

                        List<List<SE_Node>> paths = new();
                        if(!SearchClass.ContainsItems(info)){
                            continue;
                        }
                        int j = ShardDatabase.GetCurrentShardData().depth;
                        SearchClass.FindMinPaths(info.nodes.First(), -1, ref j, new(), ref paths, minNodes[x]);
                        if(j>minNodes[x]){
                            continue;
                        }

                        ItemSearchResults results = new();
                        foreach(List<SE_Node> path in paths){
                            results = new();
                            SearchClass.FindItems(path, results, info.seed);
                            if(results.winningRarityResults!=null){
                                results.winningPath = path;
                                break;
                            }
                        }

                        if(results.winningPath!=null){
                            if(j==minNodes[x]){
                                threadDicts[x].Add(i, (results.winningPath, results.winningRarityResults));
                            }else if(j<minNodes[x]){
                                threadDicts[x] = new();
                                threadDicts[x].Add(i,(results.winningPath,results.winningRarityResults));
                                minNodes[x] = j;
                            }
                        }
                        
                    }
                });
                
        // timer.Stop();

        int globalMin = ShardDatabase.GetCurrentShardData().depth+1;
        List<Dictionary<int,(List<SE_Node>,ItemRarityResults)>> validThreadDicts = new();
        for(int x = 0; x<minNodes.Count; x++){
            if(globalMin>minNodes[x]){
                globalMin = minNodes[x];
                validThreadDicts = new([threadDicts[x]]);
            }else if(globalMin==minNodes[x]){
                validThreadDicts.Add(threadDicts[x]);
            }
        }

        foreach(var dict in validThreadDicts){
            foreach(int key in dict.Keys){
                Console.WriteLine("Current Seed:"+key);
                foreach(SE_Node node in dict[key].Item1){
                    Console.WriteLine("\t"+node);
                }
                Console.WriteLine("\t\tItem info: "+dict[key].Item2);
            }
        }
        TimeSpan t1 = TimeSpan.FromMilliseconds((DateTimeOffset.Now.ToUnixTimeMilliseconds()-start));
        string answer1 = string.Format("{4:D1}d:{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", 
                t1.Hours, 
                t1.Minutes, 
                t1.Seconds, 
                t1.Milliseconds,
                t1.Days);

        Console.WriteLine("Completion time: "+answer1);
        Console.ReadLine();

    }
    static void printItems(){
        foreach(Items.Item item in Items.items){
            Console.WriteLine(item.index+": "+item.name);
        }
    }

    static int CountNodeLength(List<SE_Node> path){
        int length = 0;
        foreach(SE_Node node in path.Skip(1)){
            if(node.Type==NodeType.Default||node.Type==NodeType.Challenge){
                length++;
            }
        }
        return length;
    }
}




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

public class ItemSearchResults{
    public List<ItemRarityResults> itemRarityResults;
    public ItemRarityResults winningRarityResults;
    public List<SE_Node> winningPath;
    public bool FoundItems= false;

    public ItemSearchResults(float minRarity=0.5f){
        itemRarityResults = new List<ItemRarityResults>();
        for(float i = minRarity; i<=2; i+=.25f){
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
            toReturn += "\tItem: "+hit.item.name+" Found in: "+hit.node.Type+" At depth: "+hit.node.Depth+" Position: "+hit.node.Position+"\n";
        }
        return toReturn;
    }
}

public class ItemResultHit{
    public SE_Node node;
    public Items.Item item;
    public int roll=-1;

}


public static class SearchClass{
    public static List<int> minNodes;
    public static int searchDepth;
    public static bool isSpecific;
    public static int maxShopRolls = 2;
    public static List<Items.Item> targetItems;
    public static List<NodeType[]> targetNodes;
    public static void FindItems(List<SE_Node> path, ItemSearchResults results, int seed){
        for(int i = 0; i < results.itemRarityResults.Count; i++){
            if(FindItemsWithRarity(results.itemRarityResults[i], path, seed)){
                results.winningRarityResults = results.itemRarityResults[i];
                return;
            }
        }
    }
    

    public static void FindMinPaths(SE_Node node, int frags, ref int localMin, List<SE_Node> cPath, ref List<List<SE_Node>> paths, int globalMin){
        cPath.Add(node);
        if(node.Type==NodeType.Boss){
            if(frags<localMin){
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
            FindMinPaths(n,a, ref localMin, new(cPath), ref paths, globalMin);
        }
        
    }

    private static bool FindItemsWithRarity(ItemRarityResults itemRarityResults, List<SE_Node> path, int seed){
        List<SE_Node> nodes = path.FindAll((x)=> x.Depth<searchDepth);
        List<Items.Item> targetItemsClone = new(targetItems);
        List<NodeType[]> targetNodesClone = new(targetNodes);

        foreach(SE_Node node in nodes){

            if(findInternalNodeType(targetNodesClone,node.Type)!=-1){
                List<Items.Item> items = investigateNode(node, itemRarityResults.itemRarity,seed);
                int selectedTypeIndex = findInternalNodeType(targetNodesClone,node.Type);
                foreach(Items.Item i in items){
                    
                    if(selectedTypeIndex==-1){
                        break;
                    }
                    if(targetItemsClone.Contains(i)){
                        targetItemsClone.Remove(i);
                        targetNodesClone.RemoveAt(selectedTypeIndex);
                        ItemResultHit hit = new();
                        hit.item = i;
                        hit.node = node;
                        itemRarityResults.itemHits.Add(hit);
                        if(node.Type==NodeType.Challenge){
                            break;
                        }
                        selectedTypeIndex = findInternalNodeType(targetNodesClone,node.Type);
                    }
                }
            }
        }
        if(itemRarityResults.itemHits.Count==targetItems.Count){
            return true;
        }
        return false;
    }
    static int findInternalNodeType(List<NodeType[]> list, NodeType toFind){
        for(int i = 0; i<list.Count;i++){
            if(list[i].Contains(toFind)){
                return i;
            }
        }
        return -1;
    }

    private static List<Items.Item> investigateNode(SE_Node node, float itemRarity,int seed){
        List<Items.Item> toReturn = new();
        switch(node.Type){
            case NodeType.Shop:
                // List<int> shopItems = new();
            
                List<Items.Item> cItems = GetItems(new Random(seed+node.ID), itemRarity, node, null);
                
                toReturn.AddRange(cItems);
                for(int i = 1; i<=maxShopRolls; i++){
                    cItems = GetItems(new Random(seed+node.ID+i), itemRarity, node, null);
                    toReturn.AddRange(cItems);
                }

                break;

            case NodeType.Encounter:
                Random random = new Random(seed+node.ID);
                if(random.NextFloat()>0.5f){
                    IEnumerable<Encounters.Encounter> source = Encounters.encounters.Where<Encounters.Encounter>((x)=>x.isValid==true);
                    Encounters.Encounter randomEncounter = random.Choice<Encounters.Encounter>(source.ToArray<Encounters.Encounter>());
                    random=new Random(seed+node.ID);
                    if(randomEncounter.index==8||randomEncounter.tags.Length>0){
                        Items.Item cItem = GetRandomItem(random, new(randomEncounter.tags), randomEncounter.tagInteraction, 1,  itemRarity);
                        
                        toReturn.Add(cItem);
                    }
                }

                break;

            case NodeType.Challenge:
 
                float extraItemRarityWeight = float.Lerp(0.1f,1, GetShopRarityModifier(node.Depth,node));
                List<Items.Item> chalItems = new();
                Random random1 = new Random(seed+node.ID);
                for(int i = 0; i<3;i++){
                    chalItems.Add(GetRandomItem(random1, chalItems,extraItemRarityWeight,itemRarity));
                }
                toReturn.AddRange(toReturn);

                break;

            default:
                break;

        }
        return toReturn;
    }

    private static List<Items.Item> GetItems(Random random, float itemRarityModifier, SE_Node node, List<Items.Item> excludedItems=null){
        List<Items.Item> list = new List<Items.Item>();
        for(int i = 0; i<3; i++){
            List<Items.Item> list2 = new List<Items.Item>();
            list2.AddRange(list);
            if(excludedItems!=null){
                list2.AddRange(excludedItems);
            }
            Items.Item randomItem = GetRandomItem(random, list2, GetShopRarityModifier(node.Depth), itemRarityModifier);
            list.Add(randomItem);
        }
        return list;
    }
    private static float GetShopRarityModifier(int depth){
        float num = 0;
        float num2 = MathF.Max(0,MathF.Min(1, (float)GetDifficulty(depth)/20f * 0.3f));
        return (num+num2)* GetRunProgress(depth);
    }
    private static float GetShopRarityModifier(int depth,SE_Node node){
        float num = 0;
        float num2 = MathF.Max(0,MathF.Min(1, (float)GetDifficulty(depth,node)/20f * 0.3f));
        
        return (num+num2)* GetRunProgress(depth);
    }

    private static Items.Item GetRandomItem(Random random,List<Items.Item> excludedItems, float additionalRarity,float itemRarityModifier){
        Items.Item item = null;
        int num = 0;
        while ( num<1000&&item==null){
            Items.Item r2 = GetRandom(random, GetRarityFromPlayerWeight(random, additionalRarity+itemRarityModifier));
            if(excludedItems==null || !excludedItems.Contains(r2)){
                item = r2;
            }
            num++;
        }
        return item;
    }

    private static Items.Item GetRandomItem(Random random,List<Items.ItemTag> itemTags, Items.ItemTagInteraction tagInteraction, float additionalRarity, float itemRarityModifier){
        List<Items.Item> AcceptableItems = new List<Items.Item>();
        foreach(Items.Item item in Items.items){
            if(ItemIsAcceptable(item,itemTags,tagInteraction)){
                AcceptableItems.Add(item);
            }
        }
        return SelectItemWithRarity(AcceptableItems, GetRarityFromPlayerWeight(random, additionalRarity+itemRarityModifier), random);
    }

    private static Items.Item SelectItemWithRarity(List<Items.Item> AcceptableItems, Items.Rarity rarity, Random random){
        List<Items.Item> array;
        while(true){ //dude i hate while(trues) its so scary wtf
        array = new();
            foreach(Items.Item item in AcceptableItems){
                if(item.rarity == rarity){
                    array.Add(item);
                }
            }
            if(array.Count == 0){
                Items.Rarity? nullable = DowngradeRarity(rarity);
                if(nullable.HasValue)
                    rarity=nullable.GetValueOrDefault();
                else
                    goto label_12;
            }else{
                break;
            }
        }
        return random.Choice<Items.Item>(array);
    label_12:
        return random.Choice<Items.Item>(AcceptableItems);



        static Items.Rarity? DowngradeRarity(Items.Rarity rarity){
            switch (rarity)
            {
                case Items.Rarity.Common:
                    return new Items.Rarity?();
                case Items.Rarity.Rare:
                    return new Items.Rarity?(Items.Rarity.Common);
                case Items.Rarity.Epic:
                    return new Items.Rarity?(Items.Rarity.Rare);
                case Items.Rarity.Legendary:
                    return new Items.Rarity?(Items.Rarity.Epic);
                default:
                    throw new ArgumentOutOfRangeException(nameof (rarity), (object) rarity, (string) null);
            }
        }

    }

    private static bool ItemIsAcceptable(Items.Item item, List<Items.ItemTag> itemTags, Items.ItemTagInteraction tagInteraction){
        switch(tagInteraction){
            case Items.ItemTagInteraction.MustHaveOne:
                foreach(Items.ItemTag itemTag in itemTags){
                    if(item.tags.Contains(itemTag)){
                        return true;
                    }
                }
                return false;

            case Items.ItemTagInteraction.MustHaveAll:
                foreach(Items.ItemTag itemTag in itemTags){
                    if(!item.tags.Contains(itemTag)){
                        return false;
                    }
                }
                return true;
        }
        return true;
    }
    private static Items.Item GetRandom( Random random, Items.Rarity itemRarity){

       List<Items.Item> list = new();
        foreach(Items.Item item in Items.items){
            if(item.rarity==itemRarity){
                list.Add(item);
            }
        }
        return random.Choice(list);
       
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
    private static int GetDifficulty(int depth, SE_Node node){
        float t = GetRunProgress(depth);
        float num = float.Lerp(ShardDatabase.GetCurrentShardData().minDifficulty, ShardDatabase.GetCurrentShardData().maxDifficulty, t);
        if(node.Type==NodeType.Challenge){
            num+=5f;
        }

        return tools.RoundToInt(num);


    }

    public static bool ContainsItems(SE_Info info)
    {
        for(float i = .5f ; i<=2; i+=.25f) {
            if(FindItemsWithRarity(new ItemRarityResults(i),info.nodes,info.seed)){
                return true;
            }
        }
        return false;
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




//6 7 14 18 25 32 52 53 60
//6 7 14 18 25 32 52 53 60

public static class Encounters{
    public static Encounter[] encounters = {
        new Encounter(0, "Garden Roots", true),
 		new Encounter(1, "Memories", true),
 		new Encounter(2, "All These Sparks", true),
 		new Encounter(3, "Benefits of a Big Coat", true),
 		new Encounter(4, "Diamonds in the Rough", true),
 		new Encounter(5, "Old Flapper", true),
 		new Encounter(6, "A Small Relief", false),
 		new Encounter(7, "Wisdom of Age", false),
 		new Encounter(8, "A Moment's Respite", true),
 		new Encounter(9, "The Presence", true),
 		new Encounter(10, "Observation", true),
 		new Encounter(11, "Something From Nothing", true),
 		new Encounter(12, "Meditation?", true),
 		new Encounter(13, "A Friendship?", true, [Items.ItemTag.Wraith], Items.ItemTagInteraction.MustHaveOne),
 		new Encounter(14, "A Strange Power", false),
 		new Encounter(15, "A Strange Power", true),
 		new Encounter(16, "Place in the World", true),
 		new Encounter(17, "Breathwork", true),
 		new Encounter(18, "Receiving", false),
 		new Encounter(19, "Giving", true),
 		new Encounter(20, "Sights of Somewhere Else", true),
 		new Encounter(21, "A Rare Sighting", true, [Items.ItemTag.Heir, Items.ItemTag.Agency, Items.ItemTag.Sage], Items.ItemTagInteraction.MustHaveOne),
 		new Encounter(22, "A Shortcut", true),
 		new Encounter(23, "Harsh Training", true),
 		new Encounter(24, "Sparks!", true),
 		new Encounter(25, "Meditation", false),
 		new Encounter(26, "Balance Training", true),
 		new Encounter(27, "Gauntlet", true),
 		new Encounter(28, "Collection", true),
 		new Encounter(29, "To the Limit", true),
 		new Encounter(30, "The Test", true, [Items.ItemTag.Heir], Items.ItemTagInteraction.MustHaveOne),
 		new Encounter(31, "An Unlikely Story", true),
 		new Encounter(32, "A Great Day", false),
 		new Encounter(33, "A Great Day", true, [Items.ItemTag.Object], Items.ItemTagInteraction.MustHaveOne),
 		new Encounter(34, "A Great Day", true),
 		new Encounter(35, "A Great Day", true),
 		new Encounter(36, "You Can Do It!", true),
 		new Encounter(37, "Offer of Protection", true),
 		new Encounter(38, "In His Footsteps", true),
 		new Encounter(39, "Priorities", true),
 		new Encounter(40, "A Bet", true),
 		new Encounter(41, "Questions", true),
 		new Encounter(42, "Part of the Crew", true),
 		new Encounter(43, "Race Results", true),
 		new Encounter(44, "Race Results", true),
 		new Encounter(45, "Race Results", true, [Items.ItemTag.Researcher, Items.ItemTag.Object], Items.ItemTagInteraction.MustHaveAll),
 		new Encounter(46, "How to Do Anything", true),
 		new Encounter(47, "The Nature of Time", true),
 		new Encounter(48, "Equipment Test", true, [Items.ItemTag.Researcher, Items.ItemTag.Object], Items.ItemTagInteraction.MustHaveAll),
 		new Encounter(49, "Studying Sparks", true),
 		new Encounter(50, "Working Relationship", true),
 		new Encounter(51, "Meaning of Existence", true),
 		new Encounter(52, "Universal Destination", false),
 		new Encounter(53, "Experimental Medicine", false),
 		new Encounter(54, "Uncertainty Principles", true),
 		new Encounter(55, "An Ancient Device", true),
 		new Encounter(56, "Good Health", true),
 		new Encounter(57, "The Speed Boost Hypothesis", true),
 		new Encounter(58, "Trial and Error", true),
 		new Encounter(59, "Overcharging", true),
 		new Encounter(60, "Health Machine Emergency", false),
    };

    public class Encounter{
        public int index;
        public string name;
        public bool isValid;
        public Items.ItemTag[] tags;
        public Items.ItemTagInteraction tagInteraction;
        public Encounter(int index, string name, bool defaultValid, Items.ItemTag[] tags=null, Items.ItemTagInteraction itemTagInteraction=Items.ItemTagInteraction.None){
            this.index = index;
            this.name = name;
            this.isValid = defaultValid;
            if(tags!=null){
                this.tags = tags;
            }else{
                this.tags = [];
            }
            this.tagInteraction = itemTagInteraction;
        }

        
    }
}


public static class Items{
    public enum ItemTag
    {
        Courier = 0,
        Heir = 1,
        Sage = 2,
        Captain = 3,
        Researcher = 4,
        Wraith = 5,
        Keeper = 6,
        Agency = 20, // 0x00000014
        Snake = 21, // 0x00000015
        Hunter = 22, // 0x00000016
        Fantasy = 100, // 0x00000064
        SciFi = 101, // 0x00000065
        Weird = 102, // 0x00000066
        Object = 200, // 0x000000C8
        Intangible = 201, // 0x000000C9
        Challenge = 202, // 0x000000CA
    }
    public enum ItemTagInteraction{
        None,
        MustHaveOne,
        MustHaveAll,

    }
    public static Item[] items = {
       new Item(0, "Rocket Boots", Rarity.Common, [ItemTag.Courier,ItemTag.Captain,ItemTag.Object,ItemTag.SciFi]),
		new Item(1, "Energy Lash", Rarity.Epic, [ItemTag.Heir,ItemTag.Object,ItemTag.Courier,ItemTag.Fantasy]),
		new Item(2, "Replenishing Vial", Rarity.Common, [ItemTag.Researcher,ItemTag.Object,ItemTag.SciFi]),
		new Item(3, "Mysterious Spring", Rarity.Rare, [ItemTag.Researcher,ItemTag.Sage,ItemTag.Object,ItemTag.Weird]),
		new Item(4, "Standard Redirector", Rarity.Rare, [ItemTag.Courier,ItemTag.Captain,ItemTag.Researcher,ItemTag.Object]),
		new Item(5, "Personal Matter Stabilizer", Rarity.Rare, [ItemTag.Wraith,ItemTag.Weird,ItemTag.Researcher,ItemTag.Agency,ItemTag.SciFi]),
		new Item(6, "Time Dilation Thing", Rarity.Epic, [ItemTag.Wraith,ItemTag.Agency,ItemTag.Weird,ItemTag.Object]),
		new Item(7, "Spark Dasher", Rarity.Epic, [ItemTag.Captain,ItemTag.Wraith,ItemTag.Weird,ItemTag.Object]),
		new Item(8, "Blood Engine", Rarity.Legendary, [ItemTag.Courier,ItemTag.Wraith,ItemTag.Weird,ItemTag.Object]),
		new Item(9, "Velocity Powered Syringe", Rarity.Rare, [ItemTag.Researcher,ItemTag.Object,ItemTag.Agency,ItemTag.Weird]),
		new Item(10, "Experimental Autopilot", Rarity.Legendary, [ItemTag.Researcher,ItemTag.Agency,ItemTag.Object,ItemTag.Courier]),
		new Item(11, "Grunt's Helmet", Rarity.Rare, [ItemTag.Agency,ItemTag.Object,ItemTag.SciFi]),
		new Item(12, "Protective Medallion ", Rarity.Rare, [ItemTag.Sage,ItemTag.Heir,ItemTag.Fantasy,ItemTag.Object,ItemTag.Weird]),
		new Item(13, "Impulse Actived Stabilizer", Rarity.Rare, [ItemTag.Researcher,ItemTag.Agency,ItemTag.Object,ItemTag.SciFi]),
		new Item(14, "Painful Coil", Rarity.Epic, [ItemTag.Weird,ItemTag.Agency,ItemTag.Researcher,ItemTag.Wraith]),
		new Item(15, "Well Earned Confidence", Rarity.Rare, [ItemTag.Courier,ItemTag.Intangible,ItemTag.Captain,ItemTag.Heir]),
		new Item(16, "BOOSTR POG", Rarity.Epic, [ItemTag.Weird,ItemTag.Intangible,ItemTag.Captain,ItemTag.Object,ItemTag.Keeper]),
		new Item(17, "Pungent Herbs", Rarity.Epic, [ItemTag.Sage,ItemTag.Object,ItemTag.Fantasy]),
		new Item(18, "Shortcut", Rarity.Common, [ItemTag.Captain,ItemTag.Heir,ItemTag.Intangible,ItemTag.Keeper]),
		new Item(19, "Tight Schedule", Rarity.Epic, [ItemTag.Courier,ItemTag.Captain,ItemTag.Intangible,ItemTag.Keeper]),
		new Item(20, "Flashback", Rarity.Legendary, [ItemTag.Intangible,ItemTag.Weird,ItemTag.Wraith,ItemTag.Keeper,ItemTag.Fantasy]),
		new Item(21, "Adrenaline", Rarity.Common, [ItemTag.SciFi,ItemTag.Object,ItemTag.Agency,ItemTag.Researcher]),
		new Item(22, "Restorative Maneuver", Rarity.Common, [ItemTag.Heir,ItemTag.Courier,ItemTag.Captain,ItemTag.Intangible,ItemTag.Wraith]),
		new Item(23, "Delayed Emergency Device", Rarity.Rare, [ItemTag.SciFi,ItemTag.Researcher,ItemTag.Captain,ItemTag.Object,ItemTag.Agency]),
		new Item(24, "N-Dimensional-leaf Clover", Rarity.Rare, [ItemTag.Researcher,ItemTag.Sage,ItemTag.Weird,ItemTag.Object]),
		new Item(25, "Planar Reconfiguration", Rarity.Epic, [ItemTag.Courier,ItemTag.Researcher,ItemTag.Wraith,ItemTag.Weird,ItemTag.Intangible]),
		new Item(26, "Atomic Timepiece", Rarity.Epic, [ItemTag.Researcher,ItemTag.Keeper,ItemTag.Weird,ItemTag.Object]),
		new Item(27, "General Relativity", Rarity.Epic, [ItemTag.Researcher,ItemTag.Agency,ItemTag.Intangible,ItemTag.Weird,ItemTag.Keeper,ItemTag.Wraith]),
		new Item(28, "Overwound Pocketwatch", Rarity.Epic, [ItemTag.Courier,ItemTag.Fantasy,ItemTag.Captain,ItemTag.Weird,ItemTag.Object]),
		new Item(29, "Shiny Anchor Pin", Rarity.Rare, [ItemTag.Object,ItemTag.Fantasy,ItemTag.Captain]),
		new Item(30, "Vitamins", Rarity.Rare, [ItemTag.Courier,ItemTag.Sage,ItemTag.Researcher,ItemTag.Captain,ItemTag.Object,ItemTag.Agency]),
		new Item(31, "Heir's Determination", Rarity.Rare, [ItemTag.Heir,ItemTag.Courier,ItemTag.Intangible]),
		new Item(32, "Perpetual Motion Machine", Rarity.Common, [ItemTag.Courier,ItemTag.Researcher,ItemTag.Object,ItemTag.SciFi]),
		new Item(33, "Plutonium Coin", Rarity.Common, [ItemTag.Object,ItemTag.Agency,ItemTag.SciFi,ItemTag.Captain]),
		new Item(34, "Performance Based Health Insurance", Rarity.Rare, [ItemTag.Keeper,ItemTag.Agency,ItemTag.Intangible]),
		new Item(35, "Impact Activated Healing Drone", Rarity.Epic, [ItemTag.Agency,ItemTag.SciFi,ItemTag.Researcher]),
		new Item(36, "Leadership Pipe", Rarity.Epic, [ItemTag.Object,ItemTag.Captain]),
		new Item(37, "Karma", Rarity.Epic, [ItemTag.Intangible,ItemTag.Weird,ItemTag.Fantasy,ItemTag.Sage,ItemTag.Heir,ItemTag.Keeper]),
		new Item(38, "Brittle Breastplate", Rarity.Epic, [ItemTag.Object,ItemTag.Fantasy,ItemTag.Captain,ItemTag.Heir,ItemTag.Agency]),
		new Item(39, "Steel Hat Lining", Rarity.Epic, [ItemTag.Captain]),
		new Item(40, "Portable Harvester", Rarity.Legendary, [ItemTag.Agency]),
		new Item(41, "Otherworldly Contact", Rarity.Legendary, [ItemTag.Wraith]),
		new Item(42, "Personal Gravity Enhancer", Rarity.Epic, []),
		new Item(43, "Timeline Shifter", Rarity.Epic, []),
		new Item(44, "Recyclable Rocket", Rarity.Rare, []),
		new Item(45, "Emergency Shoes", Rarity.Rare, []),
		new Item(46, "Fragile Confidence", Rarity.Epic, []),
		new Item(47, "Dynamo Treadmill", Rarity.Common, []),
		new Item(48, "Distance-Based Health Insurance", Rarity.Common, []),
		new Item(49, "Reheated Soup", Rarity.Rare, []),
		new Item(50, "Intangibility", Rarity.Rare, []),
		new Item(51, "Greed Machine", Rarity.Rare, []),
		new Item(52, "Timeline Recalibrator", Rarity.Epic, []),
		new Item(53, "Ring Materializer", Rarity.Epic, []),
		new Item(54, "Fragile Taco", Rarity.Rare, []),
		new Item(55, "Speedy Recovery", Rarity.Rare, []),
		new Item(56, "Timeline Refactor", Rarity.Epic, []),
		new Item(57, "Shimmering Condenser", Rarity.Common, []),
		new Item(58, "Transition Slingshot", Rarity.Common, []),
		new Item(59, "Void Charger", Rarity.Common, []),
		new Item(60, "Pocket Snack", Rarity.Common, []),
		new Item(61, "Void Compressor", Rarity.Common, []),
		new Item(62, "Spark Powered Propeller", Rarity.Rare, [ItemTag.Object,ItemTag.Weird,ItemTag.Keeper,ItemTag.Agency,ItemTag.Researcher]),
		new Item(63, "Spark Furnace", Rarity.Common, [ItemTag.Object,ItemTag.Keeper,ItemTag.Captain,ItemTag.Agency]),
		new Item(64, "Mortar and Pestle", Rarity.Common, [ItemTag.Object,ItemTag.Sage,ItemTag.Keeper,ItemTag.Fantasy]),
		new Item(65, "Friendly Looking Star", Rarity.Epic, [ItemTag.Heir,ItemTag.Weird,ItemTag.Captain]),
		new Item(66, "Golden Necklace", Rarity.Common, [ItemTag.Heir,ItemTag.Captain,ItemTag.Object,ItemTag.Fantasy]),
		new Item(67, "Secret Technique Instructions", Rarity.Rare, [ItemTag.Heir,ItemTag.Captain,ItemTag.Intangible,ItemTag.Weird]),
		new Item(68, "Overcomplicated Coin", Rarity.Legendary, [ItemTag.Courier,ItemTag.Object,ItemTag.Captain,ItemTag.Keeper,ItemTag.Weird]),
		new Item(69, "Overclocked Medical Drone", Rarity.Rare, [ItemTag.Object,ItemTag.SciFi,ItemTag.Researcher,ItemTag.Agency]),
		new Item(70, "Clown Shoes", Rarity.Rare, [ItemTag.Courier,ItemTag.Weird,ItemTag.Object,ItemTag.Wraith]),
		new Item(71, "Aromatic Herbs", Rarity.Common, [ItemTag.Object,ItemTag.Sage,ItemTag.Fantasy]),
		new Item(72, "Low Grade Timeline Swapper", Rarity.Epic, [ItemTag.Object,ItemTag.SciFi,ItemTag.Researcher,ItemTag.Agency]),
		new Item(73, "Bitter Herbs", Rarity.Rare, [ItemTag.Object,ItemTag.Fantasy,ItemTag.Sage]),
		new Item(74, "Extreme Herbs", Rarity.Epic, [ItemTag.Object,ItemTag.Fantasy,ItemTag.Sage]),
		new Item(75, "Momentum Recalibrator", Rarity.Rare, [ItemTag.Object,ItemTag.SciFi,ItemTag.Researcher,ItemTag.Captain]),
		new Item(76, "Jackpot", Rarity.Rare, [ItemTag.Intangible,ItemTag.Weird,ItemTag.Captain,ItemTag.Courier,ItemTag.Heir]),
		new Item(77, "High Risk Investment", Rarity.Rare, [ItemTag.Intangible,ItemTag.Weird,ItemTag.Keeper,ItemTag.Agency,ItemTag.Captain]),
		new Item(78, "Steady Investment", Rarity.Rare, [ItemTag.Intangible,ItemTag.Object,ItemTag.Keeper,ItemTag.Agency]),
		new Item(79, "Interest", Rarity.Rare, [ItemTag.Intangible,ItemTag.Weird,ItemTag.Keeper,ItemTag.Agency]),
		new Item(80, "Dangerous Investment Scheme", Rarity.Legendary, [ItemTag.Intangible,ItemTag.Weird,ItemTag.Captain,ItemTag.Keeper,ItemTag.Agency]),
		new Item(81, "Big Spark Magnet", Rarity.Rare, [ItemTag.Object,ItemTag.Weird,ItemTag.Keeper,ItemTag.Captain,ItemTag.Researcher]),
		new Item(82, "400-leaf Clover", Rarity.Rare, [ItemTag.Object,ItemTag.Fantasy,ItemTag.Sage,ItemTag.Captain]),
		new Item(83, "Heart Shaped Mirror", Rarity.Rare, [ItemTag.Object,ItemTag.Fantasy,ItemTag.Captain,ItemTag.Keeper]),
		new Item(84, "Big Pumpkin", Rarity.Common, [ItemTag.Object,ItemTag.Fantasy,ItemTag.Courier,ItemTag.Sage,ItemTag.Heir]),
		new Item(85, "Big Squash", Rarity.Rare, [ItemTag.Object,ItemTag.Fantasy,ItemTag.Courier,ItemTag.Sage,ItemTag.Heir]),
		new Item(86, "Quick Taco", Rarity.Epic, [ItemTag.Object,ItemTag.Fantasy,ItemTag.Sage,ItemTag.Courier,ItemTag.Heir]),
		new Item(87, "Growth Potential", Rarity.Rare, [ItemTag.Intangible,ItemTag.Weird,ItemTag.Heir,ItemTag.Courier,ItemTag.Sage]),
		new Item(88, "Instant Compensation Machine", Rarity.Epic, [ItemTag.Object,ItemTag.Weird,ItemTag.Agency]),
		new Item(89, "Experimental Thrusters", Rarity.Legendary, [ItemTag.Object,ItemTag.SciFi,ItemTag.Agency,ItemTag.Researcher]),
		new Item(90, "Wingspan", Rarity.Legendary, [ItemTag.Object,ItemTag.SciFi,ItemTag.Agency,ItemTag.Researcher]),
    };
    public static bool Find(string toParse, out Item item){
        int id=-1;
        if(int.TryParse(toParse, out int p)){
            id = p; 
        }
        item = Array.Find<Item>(items, (x)=>{return x.index==id||toParse==x.name.ToLower();});
        if(item!=null){
            return true;
        }
        return false;
    }
   
    public class Item{
        public int index;
        public string name;
        public Rarity rarity;
        public ItemTag[] tags;
        public Item(int index, string name, Rarity rarity, ItemTag[] tags) {
            this.index = index;
            this.name = name;
            this.rarity = rarity;
            this.tags = tags;
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

