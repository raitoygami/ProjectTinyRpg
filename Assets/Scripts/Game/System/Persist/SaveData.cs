using System;
using System.Collections.Generic;

public partial class SaveData
{
    [Serializable]
    public class UidGeneratorState
    {
        public Dictionary<int, int> NextSeqMap = new();
    }
    public UidGeneratorState State = new();
    


}
