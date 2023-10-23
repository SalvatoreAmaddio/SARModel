namespace SARModel
{
    public static class RecordTestingGenerator
    {
        public static Task<IEnumerable<M>> RecordGeneratorAsync<M>(int num) where M : AbstractTestingTableModel<M>, new() =>
        Task.FromResult(RecordGenerator<M>(num));

        public static IEnumerable<M> RecordGenerator<M>(int num) where M : AbstractTestingTableModel<M>, new()
        {
            int id = 0;
            while (num > 0)
            {
                M record = new();
                record.ID = ++id;
                num--;
                yield return record;
            }
        }
    }
}
