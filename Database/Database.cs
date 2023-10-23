using MySqlConnector;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace SARModel
{
    public abstract class AbstractDatabaseTable<M> : IDB where M : AbstractTableModel<M>, new()
    {
        public IDbConnection? connection { get; set; }
        protected IDbCommand? Command;
        protected IDbTransaction? Transaction;
        readonly StandardSQLConstructor StandardSQLConstructor = new(typeof(M));
        public bool IsConnected { get => connection!=null && connection.State.Equals(ConnectionState.Open); }
        M _model { get; } = new();
        public RecordSource<M> RecordSource { get; private set; } = new ();
        public IRecordSource DataSource { get => RecordSource; }
        public Type ModelType { get => _model.GetType(); }
        public AbstractDatabaseTable() => EstablishConnection();
        abstract protected void EstablishConnection();
        public void OpenConnection() => connection?.Open();
        public void CloseConnection() => connection?.Close();
        public void Reconnect()
        {
            if (IsConnected) CloseConnection();
            EstablishConnection();
            OpenConnection();
        }
        public bool IsConnectionStateGood()
        {
            if (connection == null) return false;
         
            return connection.State switch
            {
                ConnectionState.Open => true,
                ConnectionState.Closed or ConnectionState.Broken => false,
                ConnectionState.Connecting or ConnectionState.Executing or ConnectionState.Fetching => true,
                _ => false,
            };
        }
        protected IEnumerable<M> FetchData(IDataReader? reader)
        {
            if (reader == null) throw new Exception();
            while (reader.Read()) yield return _model.GetRecord(reader);
        }
        public void Select()
        {
            if (!IsConnectionStateGood()) throw new Exception("Connection is not open or broken;");
            NewCommand(_model.SQLStatements(SQLType.SELECT), (DbConnection?)connection);
            RecordSource = new(FetchData(Command?.ExecuteReader()));
            Command?.Dispose();
        }
        public async Task GetTable()
        {
            await StandardSQLConstructor.Run();
            _model.SetStandardSQLConstructor(StandardSQLConstructor);
            connection?.Open();
            Select();
            connection?.Close();
        }
        public IEnumerable<Task> SetForeignKeys() =>DataSource.Select<object, Task>(item =>((AbstractTableModel<M>)item).SetForeignKeys());
        public int Update(object? _record) => AlterRecord((M?)_record, "update", SQLType.UPDATE);
        public int Delete(object? _record) => AlterRecord((M?)_record, "delete", SQLType.DELETE);
        public int Insert(object? _record) => AlterRecord((M?)_record, "insert", SQLType.INSERT);
        protected long LastInsertedID()
        {
            if (!IsConnectionStateGood()) throw new Exception("Connection is not open or broken;");
            
            if (this is SQLiteTable<M>)
                NewCommand("SELECT last_insert_rowid();", connection);
            else
                NewCommand("SELECT LAST_INSERT_ID();", connection);

            object? val = Command?.ExecuteScalar();
            Command?.Dispose();
            if (val is not null) return (long)val;
            return 0;
        }
        protected abstract void NewCommand(string sqlstatement,IDbConnection? connection);
        protected int AlterRecord(M? _record, string exception, SQLType _sqlType)
        {
            if (_record == null) throw new ArgumentNullException($"Cannot {exception} a null record.");
            if (!IsConnectionStateGood()) throw new Exception("Connection is not open or broken;");
            _record.SetStandardSQLConstructor(StandardSQLConstructor);
            NewCommand(_record.SQLStatements(_sqlType), connection);
            Params param = new(Command?.Parameters);
            _record.Params(param);
            switch(this)
            {
                case SQLiteTable<M>:
                    param.RunParamsSQLite();
                    break;
                case MySQLTable<M>:
                    param.RunParamsMYSQL();
                    break;
            }

            int? rows = Command?.ExecuteNonQuery();
            Command?.Parameters.Clear();
            Command?.Dispose();
            if (rows == null) return 0;
            switch (_sqlType)
            {
                case SQLType.INSERT:
                    _record.SetPrimaryKey(LastInsertedID());
                    RecordSource.AddRecord(_record);
                    break;
                case SQLType.DELETE:
                    RecordSource.RemoveRecord(_record);
                    break;
                case SQLType.UPDATE:
                    RecordSource.UpdateRecord(_record);
                    break;
            }
            _record.IsDirty = false;
            return (int)rows;
        }
        public int RunStatement(string sql)
        {
            NewCommand(sql, connection);
            int? rows = Command?.ExecuteNonQuery();
            Command?.Dispose();
            return (rows == null) ? 0 : (int)rows;
        }
        public void StartTransaction()
        {
            OpenConnection();
            NewCommand();
            if (Command == null) return;
            RunStatement("SET autocommit = 0;");
            Transaction = connection?.BeginTransaction();
            Command.Connection = (DbConnection?)connection;
        }
        protected abstract void NewCommand();
        public object? InsertTransaction(object? _record) => RunTransaction((M?)_record, "insert", SQLType.INSERT);
        public object? UpdateTransaction(object? _record) => RunTransaction((M?)_record, "update", SQLType.UPDATE);
        public object? DeleteTransaction(object? _record) => RunTransaction((M?)_record, "delete", SQLType.DELETE);
        M? RunTransaction(M? _record, string exception, SQLType _sqlType)
        {
            if (_record == null) throw new ArgumentNullException($"Cannot {exception} a null record.");
            if (!IsConnectionStateGood()) throw new Exception("Connection is not open or broken;");
            if (Command == null) throw new Exception("Command is null, StartTransaction() might not have been called.");
            _record.SetStandardSQLConstructor(StandardSQLConstructor);
            Command.CommandText = _record?.SQLStatements(_sqlType);
            Command.Transaction = Transaction;
            Params param = new(Command.Parameters);
            _record?.Params(param);
            Command.ExecuteScalar();
            Command.Parameters.Clear();
            Command.Dispose();
            _record!.IsDirty = false;
            if (_sqlType.Equals(SQLType.INSERT)) _record?.SetPrimaryKey(LastInsertedID());            
            return _record;
        }
        public void CommitTransaction()
        {
            Transaction?.Commit();
            Command?.Dispose();
            RunStatement("SET autocommit = 1;");
            CloseConnection();
        }
        public void RollBack()
        {
            Transaction?.Rollback();
            Command?.Dispose();
            RunStatement("SET autocommit = 1;");
            CloseConnection();
        }
        public override string ToString() => $"Database<{_model.GetType().Name}>";
        public static void CopyStream(Stream inputStream, Stream outputStream)
        {
            CopyStream(inputStream, outputStream, 4096);
        }
        static void CopyStream(Stream inputStream, Stream outputStream, int bufferLength)
        {
            var buffer = new byte[bufferLength];
            int bytesRead;
            while ((bytesRead = inputStream.Read(buffer, 0, bufferLength)) > 0)
            {
                outputStream.Write(buffer, 0, bytesRead);
            }
        }
    }
    public class MySQLTable<M> : AbstractDatabaseTable<M> where M : AbstractTableModel<M>, new()
    {
        protected override void EstablishConnection()
        {
            if (string.IsNullOrEmpty(DatabaseManager.ConnectionString)) throw new NotImplementedException("Connection String is empty");
            connection = new MySqlConnection(DatabaseManager.ConnectionString);
        }

        protected override void NewCommand(string sqlstatement, IDbConnection? connection)=>
        Command = new MySqlCommand(sqlstatement, (MySqlConnection?)connection);

        protected override void NewCommand()=>
        Command = new MySqlCommand();
    }
    public class SQLiteTable<M> : AbstractDatabaseTable<M> where M : AbstractTableModel<M>, new()
    {
        protected override void EstablishConnection()
        {
                DatabaseManager.SQLite = Sys.DLLs.First(s=>s.ResourceName.ToLower().Contains("myass")).Reload();
                connection = (IDbConnection?)DatabaseManager.SQLite?.CreateInstance("System.Data.SQLite.SQLiteConnection");
                if (connection != null)
                    connection.ConnectionString = DatabaseManager.SQLiteConnectionString();
        }

        protected override void NewCommand(string? sqlstatement, IDbConnection? connection)
        {
            Command = connection?.CreateCommand();
            if (Command == null) return;
            Command.CommandText = sqlstatement;
        }

        protected override void NewCommand() =>
        Command = connection?.CreateCommand();
    }
    public class SQLiteParam 
    {
        public IDbDataParameter? def;
        public SQLiteParam(ValueParamter valueParamter)
        {
            def = (IDbDataParameter?)DatabaseManager.SQLite?.CreateInstance("System.Data.SQLite.SQLiteParameter");
            if (def == null) throw new Exception();
            def.ParameterName = valueParamter.ParameterName;
            def.Value = valueParamter.Value;
        }
        
    }
    public class Params : List<ValueParamter>
    {
        readonly IDataParameterCollection? parameters;

        public Params(IDataParameterCollection? _parameters)=>parameters = _parameters;
        
        public void RunParamsSQLite()
        {
            foreach (ValueParamter valueParamter in this)
            {
                var x = new SQLiteParam(valueParamter);
                parameters?.Add(x.def);
            }
        }

        public void RunParamsMYSQL()
        {
            foreach (var x in this)
            {
                parameters?.Add(new MySqlParameter(x.ParameterName, x.Value));
            }
        }

        public void AddProperty(object? value, string paramName) =>
        Add(new(value, paramName));

        public void AddEnumerableProperty<T>(IEnumerable source, params string[] paramNames)
        {
            var array = source.OfType<T>().ToArray();

            for (int i = 0; i < array.Length; i++)
            {
                    Add(new(array[i], paramNames[i]));
            }
        }


    }
    public class ValueParamter
    {
        public ValueParamter(object? value,string paramName)
        {
            ParameterName = $"@{paramName}";
            Value = value;
        }

        public string ParameterName { get; set; }
        public object? Value { get; set; }

    }

}
