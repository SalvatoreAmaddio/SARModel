using System.Data;

namespace SARModel
{
    public interface IDB
    {
        public IDbConnection? connection { get; set; }
        public Type ModelType { get; }
        public Task GetTable();
        public void Select();
        public int Insert(object? _record);
        public int Update(object? _record);
        public int Delete(object? _record);
        public int RunStatement(string sql);
        public void OpenConnection();
        public void CloseConnection();
        public object? InsertTransaction(object? _record);
        public object? UpdateTransaction(object? _record);
        public object? DeleteTransaction(object? _record);
        public IRecordSource DataSource { get; }
        public IEnumerable<Task> SetForeignKeys();
    }

}
