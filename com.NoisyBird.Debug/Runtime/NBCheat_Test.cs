namespace NoisyBird.Debug
{
    public enum NBCheatCategory_Test
    {
        None,
        Group1,
        Group2,
        Group3,
    }
    
    public class NBCheat_Test
    {
        [NBCheat(NBCheatCategory_Test.Group1, 1)]
        public static string Debug_Text { get; set; }
        
        [NBCheat(NBCheatCategory_Test.Group1, 1)]
        public static void Test_DebugLog()
        {
            Debug.Log(DebugType.Resource, Debug_Text);
        }
        
        [NBCheat(NBCheatCategory_Test.Group1)]
        public static void Test_DebugLog2()
        {
            Debug.Log("test debug2");
        }
        
        [NBCheat(NBCheatCategory_Test.Group3)]
        public static void Test_DebugLog3()
        {
            Debug.Log("test debug3");
        }
    }
}