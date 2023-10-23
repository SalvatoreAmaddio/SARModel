namespace SARModel
{
    public static class Model
    {        
        static readonly List<object> Vars = new();
        public static void Add(object obj)=>Vars.Add(obj);
        public static void Add(params object[] objs) 
        {
            foreach (var obj in objs) Vars.Add(obj);
        }
        public static void ProduceDeploymentFiles(SoftwareInfo softwareInfo)
        {
            ProduceDeploymentFiles deploymentFiels = new(softwareInfo);
            deploymentFiels.Produce();
        }
        public static string Capitalise(this string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            str = str.Trim().ToLower();
            string[] stringChunks = str.Split(' ');
            string firstLetter = string.Empty;
            string restOfString = string.Empty;
            string chunk = string.Empty;

            for (int i=0; i < stringChunks.Length; i++) 
            {
                chunk = stringChunks[i];
                firstLetter = chunk.FirstOrDefault().ToString().ToUpper();
                restOfString = chunk.Remove(0, 1);
                stringChunks[i] = $"{firstLetter}{restOfString}";
            }

            return string.Join(' ',stringChunks);
        }

        //public static IRecordSource? RecordSource<T>() => Vars.OfType<IRecordSource>().ToList().FirstOrDefault(s => s.ModelType.Equals(typeof(T)));
        public static T Get<T>(int index) => (T)Vars[index];
        public static T? Get<T>() => (T?)Vars.FirstOrDefault(s => s.GetType().Equals(typeof(T)));
        public static List<IRecordSource> Sources() => Vars.OfType<IRecordSource>().ToList();
        public static List<IAbstractModel> Models() => Vars.OfType<IAbstractModel>().ToList();

    }
}