using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace SARModel
{
    public enum SQLType
    {
        SELECT,
        UPDATE,
        INSERT,
        DELETE,
    }

    #region Attr
    [AttributeUsage(AttributeTargets.All)]
    public abstract class TableSchema : Attribute
    {
        readonly string Name = string.Empty;
        public TableSchema([CallerMemberName] string memberName = "") => Name = memberName;
        public override string? ToString() => Name;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnName : TableSchema
    {
        public ColumnName([CallerMemberName] string memberName = "") : base(memberName)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnNameArray : ColumnName
    {
        readonly IEnumerable<string> names;
        readonly string tostring;

        public ColumnNameArray(params string[] columns) : base()
        {
            names = columns;
            StringBuilder sb = new();
            foreach (var s in names) sb.Append($"{s},");
            int index = sb.Length - 2;
            sb.Remove(index, 2);
            tostring = sb.ToString();
        }

        public ColumnNameArray(string fieldName, int n)
        {
            names = Compose(fieldName, n);
            StringBuilder sb = new();
            foreach (var s in names) sb.Append($"{s}, ");
            int index = sb.Length - 2;
            sb.Remove(index, 2);
            tostring = sb.ToString();
        }

        static IEnumerable<string> Compose(string fieldName, int n)
        {
            for (int i = 1; i <= n; i++)
                yield return $"{fieldName}{i}";            
        }

        public string ForInsert()
        {
            StringBuilder sb = new();
            foreach (var field in names) sb.Append($"@{field}, ");
            return sb.ToString();
        }

        public string ForUpdate()
        {
            StringBuilder sb = new();
            foreach (var field in names) sb.Append($"{field} = @{field}, ");
            return sb.ToString();
        }

        public override string? ToString() => tostring;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class FK : ColumnName
    {
        public FK([CallerMemberName] string memberName = "") : base(memberName)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PK : TableSchema
    {
        public PK([CallerMemberName] string memberName = "") : base(memberName)
        {
        }
    }
    #endregion

    #region SQLMaker 
    public class StandardSQLConstructor
    {
        readonly Type type;
        readonly PropertyInfo[] props;
        private IEnumerable<FK>? FKs;
        readonly StringBuilder sb = new();
        public string Select { get; set; } = string.Empty;
        public string Update { get; set; } = string.Empty;
        public string Delete { get; set; } = string.Empty;
        public string Insert { get; set; } = string.Empty;
        string TableName { get; set; }
        public StandardSQLConstructor(Type abstractmodel)
        {
            type = abstractmodel;
            TableName = type.Name;
            props = type.GetProperties();
        }

        Task<string> CreateSelectStatement() => Task.FromResult($"SELECT * FROM {TableName};");
        Task<string> CreateDeleteStatement(IEnumerable<PK> PKs) => Task.FromResult($"DELETE FROM {TableName} WHERE {PKs?.First()}=@{PKs?.First()};");
        Task<string> CreateInsertStatement(IEnumerable<ColumnName> Columns)
        {
            sb.Clear();
            sb.Append($"INSERT INTO {TableName} (");

            foreach (ColumnName column in Columns) sb.Append($"{column}, ");

            int index = sb.Length - 2;
            sb.Remove(index, 2);
            sb.Append(") VALUES (");

            foreach (ColumnName column in Columns) sb.Append((column is ColumnNameArray c) ? c.ForInsert() : $"@{column}, ");

            index = sb.Length - 2;

            sb.Remove(index, 2);

            sb.Append(')');

            return Task.FromResult(sb.ToString());
        }
        Task<string> CreateUpdateStatement(IEnumerable<ColumnName> Columns, IEnumerable<PK> PKs)
        {
            sb.Clear();
            sb.Append($"UPDATE {TableName} SET ");
            foreach (ColumnName column in Columns) sb.Append((column is ColumnNameArray c) ? c.ForUpdate() : $"{column} = @{column}, ");
            int index = sb.Length - 2;
            sb.Remove(index, 2);
            sb.Append($" WHERE {PKs?.First()} =@{PKs?.First()};");
            return Task.FromResult(sb.ToString());
        }
        Task<IEnumerable<ColumnName>> GetColumns() => Task.FromResult(Get<ColumnName>());
        Task<IEnumerable<PK>> GetPKs() => Task.FromResult(Get<PK>());
        Task<IEnumerable<FK>> GetFKs() => Task.FromResult(Get<FK>());

        IEnumerable<T> Get<T>()
        {
            foreach (PropertyInfo prop in props)
            {
                List<T> attributes = prop.GetCustomAttributes(true).OfType<T>().ToList();
                foreach (T column in attributes)
                    yield return column;
            }
        }

        public async Task Run()
        {
            var columnTask = GetColumns();
            var PKTask = GetPKs();
            var FKTask = GetFKs();

            await Task.WhenAll(columnTask, PKTask, FKTask);

            IEnumerable<ColumnName> Columns = columnTask.Result;
            IEnumerable<PK> PKs = PKTask.Result;
            FKs = FKTask.Result;

            var delete = CreateDeleteStatement(PKs);
            var update = CreateUpdateStatement(Columns, PKs);
            var insert = CreateInsertStatement(Columns);
            var select = CreateSelectStatement();

            await Task.WhenAll(select, delete, update,insert);

            Select = select.Result;
            Delete = delete.Result;
            Update = update.Result;
            Insert = insert.Result;
        }
    }
    #endregion

}
