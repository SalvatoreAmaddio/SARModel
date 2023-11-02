
using System.Collections;

namespace SARModel {
    public interface IRecordSource : IEnumerable<IAbstractModel>, IEnumerable {

        #region Properties
        public string RecordsPositionDisplayer { get; set; }
        public IRecordsOrganizer? Filter {get; set;}
        public long SourceID { get; }
        public int RecordCount { get; }
        public int CurrentPosition { get; }
        public bool IsBOF { get; }
        public bool IsEmpty { get; }
        public bool HasMoved { get; }
        public bool IsEOF { get; }
        public bool IsNewRecord { get; }
        public bool IsChild { get; }
        public bool AllowNewRecord { get; set; }
        public IRecordSource? Parent { get; set; }
        #endregion

        #region CRUD
        public void AddRecord(IAbstractModel? record);
        public void RemoveRecord(IAbstractModel? record);
        public void UpdateRecord(IAbstractModel? record);
        public void ReplaceData(IEnumerable records);
        #endregion

        #region CurrentRecord
        public IAbstractModel CurrentRecord();
        public void SetCurrentRecord(IAbstractModel? record);
        #endregion
        public IRecordSource Copy();
        public void SetFilter(IRecordsOrganizer recordsOrganizer);
        public object Get(int index);

        #region RequeryOptions
        public void Requery();
        public void Refilter();
        public void Reorder();
        #endregion

        #region Children
        public void RequeryChildren();
        public void ReorderChildren();
        public void RefilterChildren();
        public IRecordSource MakeChild();
        public void AddChild(IRecordSource child);
        public IRecordSource GetChild(long id);
        #endregion

        #region Reporting
        public void PrintRecords();
        public void PrintMe();
        public void PrintReportStatus();
        public void PrintCurrentRecordStatus();
        public string ReportStatus();
        public string ReportCurrentRecordInfo();
        #endregion

        #region Movement
        public bool GoNewRecord();
        public bool GoNext();
        public bool GoPrevious();
        public bool GoFirst();
        public bool GoLast();
        #endregion
        IEnumerable<M> Where<M>(Func<M, bool> predicate, bool replace = true) where M : IAbstractModel;

        #region Events
        public event EventHandler<OnRecordChangedEvtArgs>? OnRecordChanged;
        public event EventHandler<RecordMovedEvtArgs>? OnRecordMoved;
        public event EventHandler<RecordMovingEvtArgs>? OnRecordMoving;
        #endregion

        public static RecordSource<T> InitSource<T>() where T : AbstractTableModel<T>, new() 
        {
            RecordSource<T> source = new((IEnumerable<T>)DatabaseManager.GetDatabaseTable<T>().DataSource);
            DatabaseManager.AddChild<T>(source);
            return source;
        }
    }
    public abstract class AbstractRecordsOrganizer<M> : IRecordsOrganizer where M : AbstractTableModel<M>, new()
    {
        #region Properties
        public long SourceID { get; set; }

        /// <summary>
        /// <code>
        /// OriginalSource => DatabaseManager.GetDatabaseTable&#x3c;objectToLookTo>().DataSource;
        /// </code>
        /// </summary>
        protected virtual IRecordSource OriginalSource => DatabaseManager.GetDatabaseTable<M>().DataSource;
        public object? SelectedItem { get; set; }
        public bool SelectFirst { get; set; } = false;
        private object? DataContext { get; set; }
        #endregion

        #region Event
        public event EventHandler<RequeryEventArgs>? OnRequery;
        #endregion

        #region Constructors
        public AbstractRecordsOrganizer() 
        { 
            
        }
        public AbstractRecordsOrganizer(long id) : this() => SourceID = id;
        #endregion

        #region AbstractMethods
        public abstract bool FilterCriteria(IAbstractModel record);
        public virtual void OnFilter(IRecordSource FilteredSource) => FilteredSource.ReplaceData(OriginalSource.Where(FilterCriteria));
        public virtual void OnReorder(IRecordSource FilteredSource) => FilteredSource.ReplaceData(FilteredSource.OrderBy(s => s.ToString()));
        #endregion

        #region Methods
        public void Requery() => OnRequery?.Invoke(this, new(this));
        private IRecordSource FindSource()=>
        (SourceID == 0) ? OriginalSource.MakeChild() : OriginalSource.GetChild(SourceID);

        public IRecordSource GetOrganisedSource() => OriginalSource.GetChild(SourceID);
        public void Run()
        {
            IRecordSource FilteredSource=FindSource();
            FilteredSource.Filter = this;
            OnFilter(FilteredSource);
            OnReorder(FilteredSource);
            if (SelectFirst) SelectedItem = FilteredSource.First();
            else SelectedItem = null;
        }

        public M? GetDataContext<M>() => (M?)DataContext;
        public void SetDataContext(object? _dataContext) => DataContext = _dataContext;
        #endregion
    }

    public interface IRecordsOrganizer
    {
        public long SourceID { get; set; }
        public object? SelectedItem { get; set; }

        /// <summary>
        /// <code>
        /// Model? dataContext = GetDataContext&#x3c;Model>(); //object to filter by
        /// return OriginalSource.Any&#x3c;OriginalSourceModel>(s=>(your condition));
        /// </code>
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public bool FilterCriteria(IAbstractModel record);
        public void OnFilter(IRecordSource FilteredSource);

        /// <summary>
        /// return FilteredSource.ReplaceData(FilteredSource.OrderBy(s => s.ToString()));
        /// </summary>
        /// <param name="source"></param>
        public void OnReorder(IRecordSource source);
        public void Run();
        public M? GetDataContext<M>();
        public void SetDataContext(object _dataContext);
        public void Requery();

        public IRecordSource GetOrganisedSource();

        public event EventHandler<RequeryEventArgs>? OnRequery;
    }

    public class RequeryEventArgs : EventArgs
    {
        private object Source { get; set; }
        public RequeryEventArgs(object source) =>Source = source;
    }

    #region Enums
    public enum RecordAction
    {
        ADD,
        UPDATE,
        DELETE,
    }
    public enum RecordMovement
    {
        FIRST,
        PREVIOUS,
        NEXT,
        LAST,
        NEW
    }
    public enum SourceOrder
    {
        ASC,
        DESC,
    }
    #endregion

    #region RecordChangedEventArgs
    public abstract class AbstractRecordChangedEvtArgs
    {
        public object? Record;
        protected RecordAction RecordAction { get; set; } = RecordAction.ADD;

    }
    public class OnBeforeRecordChangedEvtArgs : AbstractRecordChangedEvtArgs
    {
        public bool GoingToAddRecord { get => RecordAction.Equals(RecordAction.ADD); }
        public bool GoingToUpdateRecord { get => RecordAction.Equals(RecordAction.UPDATE); }
        public bool GoingToDeleteRecord { get => RecordAction.Equals(RecordAction.DELETE); }

        public bool Cancel { get; set; } = false;
        public OnBeforeRecordChangedEvtArgs(object _record, RecordAction _recordAction = RecordAction.ADD)
        {
            Record = _record;
            RecordAction = _recordAction;
        }
        public OnBeforeRecordChangedEvtArgs() { }

    }
    public class OnRecordChangedEvtArgs : AbstractRecordChangedEvtArgs
    {
        public bool IsOnDeleteCascade { get => RecordDeleted && Record != null; }
        public bool RecordAdded { get => RecordAction.Equals(RecordAction.ADD); }
        public bool RecordUpdate { get => RecordAction.Equals(RecordAction.UPDATE); }
        public bool RecordDeleted { get => RecordAction.Equals(RecordAction.DELETE); }
        public OnRecordChangedEvtArgs(object _record, RecordAction _recordAction = RecordAction.ADD)
        {
            Record = _record;
            RecordAction = _recordAction;
        }
        public OnRecordChangedEvtArgs() { }

        public static Task DeleteOnCascadeLoop(IEnumerable? range, IRecordSource? source)
        {
            if (range == null || source == null) return Task.CompletedTask;
            foreach (var record in range) source.RemoveRecord((IAbstractModel?)record);
            return Task.CompletedTask;
        }

    }
    #endregion

    #region RecordMovementArgs
    abstract public class RecordMovementArgs : EventArgs
    {
        public IAbstractModel? Record;
        public RecordMovement? Movement;

        public RecordMovementArgs() { }
        public RecordMovementArgs(IAbstractModel? record, RecordMovement? movement) 
        {
            Record=record;
            Movement=movement;
        }
    }
    public class RecordMovingEvtArgs : RecordMovementArgs
    {
        public bool Cancel = false;
        public bool IsDirty() => Record != null && Record.IsDirty;
        public bool IsNew() => Record != null && Record.IsNewRecord;
        public bool IsNewAndDirty() => Record != null && Record.IsDirty && Record.IsNewRecord;

        public RecordMovingEvtArgs() { }

        public RecordMovingEvtArgs(IAbstractModel? record, RecordMovement? movement) : base(record, movement)
        {
        }
    }
    public class RecordMovedEvtArgs : RecordMovementArgs
    {        
        public RecordMovedEvtArgs() { }
        public RecordMovedEvtArgs(IAbstractModel? record, RecordMovement? movement) : base(record, movement)
        {
        }

        public bool WentToNewRecord { get => Movement.Equals(RecordMovement.NEW); }
        public bool WentToNextRecord { get => Movement.Equals(RecordMovement.NEXT); }
        public bool WentToPreviousRecord { get => Movement.Equals(RecordMovement.PREVIOUS); }
        public bool WentToFirstRecord { get => Movement.Equals(RecordMovement.FIRST); }
        public bool WentToLastRecord { get => Movement.Equals(RecordMovement.LAST); }

    }
    #endregion

}
