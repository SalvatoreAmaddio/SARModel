using System.Data;
using System.Runtime.CompilerServices;

namespace SARModel
{
    public abstract class AbstractUser : AbstractModel
    {
        int _userid;
        public int UserID { get=>_userid; set=>Set(ref value, ref _userid); }

        string _username=string.Empty;
        public string UserName { get => _username; set => Set(ref value, ref _username); }

        string _password=string.Empty;
        public string Password
        {
            get => _password; set => Set(ref value, ref _password);
        }

        public override int ObjectHashCode => HashCode.Combine(UserID);
        public override bool IsEqualTo(object? obj)=>obj != null && obj is AbstractUser user && user.UserID == UserID;
        public override string ObjectName => UserName;
    }
    public abstract class AbstractTestingTableModel<M> : AbstractTableModel<M>, ITestingTableModel where M : new()
    {
        abstract public int ID { get; set; }
        public string Name { get=>$"{GetType().Name}{ID}"; }
    }
    public abstract class AbstractTableModel<M> : AbstractModel where M : new()
    {
        private StandardSQLConstructor? standardSQLConstructor;
        public static T GetFK<T>(T fk) where T : AbstractTableModel<T>, new()=>
        (T)DatabaseManager.GetDatabaseTable<T>().DataSource.First(s => s.Equals(fk));

        /// <summary>
        /// This method convert a Database record into a object of type M.
        /// <para>For Example:</para>
        /// create a constructor as follow:
        /// <code>
        /// public Constructor(IDataReader reader)
        /// {
        ///    _backProp1 = reader.GetInt64(0);
        ///    _backProp2 = reader.GetString(1);
        ///    ...
        ///    _backPropN = reader.GetString(3);
        ///}
        /// </code>
        /// Then override the method as:
        /// <code>
        /// public override M GetRecord(IDataReader reader) => new(reader);
        /// </code>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A Record of type M</returns>
        public abstract M GetRecord(IDataReader reader);

        /// <summary>
        /// This method is used to bind Properties values to CRUD query's parameters.
        /// <para>For Example:</para>
        /// <code>
        /// param.AddProperty(Property1, nameof(Property1));
        /// param.AddProperty(Property2, nameof(Property2));
        /// ...
        /// param.AddProperty(PropertyN, nameof(PropertyN));
        /// </code>
        /// If you have objects that rapresent a foreign key:
        /// <code>
        ///  param.AddProperty(object?.Id, "ForeignKeyName");
        /// </code>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <remarks>
        /// N.B: the paramName parameter must match the column name in the SQL Table.
        /// </remarks>
        /// <param name="param"></param>
        public abstract void Params(Params param);

        /// <summary>
        /// This Task helps to fill up all those objects that serves as a ForeignKey once the Select Statement has run.
        /// <para>For Example:</para>
        /// <code>
        ///  if (_object != null) _object.Property1 = GetFK(_object).Property1;
        ///   return Task.CompletedTask;
        /// </code>
        /// see also the <see cref="GetFK"/> method.
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        /// <returns>A Task</returns>
        public abstract Task SetForeignKeys();

        /// <summary>
        /// This method is used into the Insert operations so that the newly inserted record will have its primary key coming from the Database 
        /// <para>Implement as follow</para>
        /// <code>
        /// public override void SetPrimaryKey(long id) => _Id = id;
        /// </code>
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        /// <param name="id"></param>
        public abstract void SetPrimaryKey(long id);
        public virtual string SQLStatements(SQLType _sqlType)
        {
            if (standardSQLConstructor == null) return string.Empty;
            return _sqlType switch
            {
                SQLType.SELECT => standardSQLConstructor.Select,
                SQLType.INSERT => standardSQLConstructor.Insert,
                SQLType.DELETE => standardSQLConstructor.Delete,
                SQLType.UPDATE => standardSQLConstructor.Update,
                _ => string.Empty,
            };
        }
        public void SetStandardSQLConstructor(StandardSQLConstructor _standardSQLConstructor) => standardSQLConstructor = _standardSQLConstructor;
    }

    public abstract class AbstractModel : AbstractNotifier, IAbstractModel 
    {
        bool _isdirty = false;
        public bool IsDirty { get=>_isdirty; set=>Set(ref value,ref _isdirty); }
        public abstract bool IsNewRecord { get; }
        public override void Set<T>(ref T value, ref T _backprop, [CallerMemberName] string propName = "") where T : default
        {
            base.Set(ref value, ref _backprop, propName);
            if (propName.Equals(nameof(IsDirty))) return;
            IsDirty = true;
        }
        public override void Undo()
        {
            base.Undo();
            IsDirty = false;
        }
        public abstract string ObjectName { get; }
        public abstract int ObjectHashCode { get; }

        /// <summary>To implement as follow:
        /// <code>
        /// obj is ClassType clsType <![CDATA[&&]]> clsType.Id == Id;
        /// </code>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public abstract bool IsEqualTo(object? obj);
        public override string ToString() => ObjectName;
        public override int GetHashCode() => ObjectHashCode;
        public override bool Equals(object? obj) => IsEqualTo(obj);
        public abstract bool CanSave();
        public virtual void Clear()=>throw new NotImplementedException("To implement");
    }           
}