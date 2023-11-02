using Microsoft.Office.Interop.Word;
using MvvmHelpers;
using System.Collections;
using System.Data;
using System.Text;

namespace SARModel
{
    public class RecordSource<M> : ObservableRangeCollection<M>, IRecordSource where M : AbstractModel, new()
    {
        #region Private
        private RecordMovingEvtArgs RecordMovingArgs = new ();
        private readonly StringBuilder sb = new();
        private List<IRecordSource> Children { get; } = new();
        private int LastIndex => (Count == 0) ? -1 : Count - 1;
        private static long Id;
        private long _sourceID;
        private IRecordsOrganizer? _filter;
        private string _recordsPositionDisplayer=string.Empty;
        bool CanGoNext { get=> CurrentPosition != LastIndex && !IsNewRecord; }
        bool CanGoNew { get => (AllowNewRecord && !IsNewRecord) || (AllowNewRecord && !HasMoved); }
        private M _currentRecord = new();
        private M Current
        { 
            get=>_currentRecord;
            set {
                 _currentRecord = value;
                 OnPropertyChanged(new(nameof(CurrentPosition)));
                 OnPropertyChanged(new(nameof(IsBOF)));
                 OnPropertyChanged(new(nameof(IsEOF)));
                 OnPropertyChanged(new(nameof(HasMoved)));
                 OnPropertyChanged(new(nameof(IsNewRecord)));
                 UpdateRecordsPositionDisplayer();
            }
        }
        #endregion

        #region IRecordSource Properties
        public string RecordsPositionDisplayer 
        { 
            get=> _recordsPositionDisplayer; 
            set=> _recordsPositionDisplayer=value; 
        }

        public IRecordsOrganizer? Filter 
        { 
            get=>_filter;
            set 
            {
                _filter = value;
                if (_filter != null)
                _filter.SourceID = _sourceID;
            }  
        }
        public long SourceID { get=>_sourceID; }
        public int RecordCount { get => Count; }
        public int CurrentPosition { get=> (Current == null) ? -1 : IndexOf(Current); }
        public bool IsBOF { get => RecordCount>=0 && CurrentPosition==0; }
        public bool IsEOF { get => RecordCount >= 0 && CurrentPosition == LastIndex && !IsEmpty; }
        public bool HasMoved { get => IsBOF != IsEOF; }
        public bool IsNewRecord { get => Current != null && Current.IsNewRecord; }
        public bool IsEmpty { get => RecordCount == 0 && CurrentPosition < 0; }
        public IRecordSource? Parent { get; set; }
        public bool IsChild { get=>Parent!=null; }
        public bool AllowNewRecord { get; set; } = true;
        #endregion

        #region Events
        public event EventHandler<RecordMovedEvtArgs>? OnRecordMoved;
        public event EventHandler<RecordMovingEvtArgs>? OnRecordMoving;
        public event EventHandler<OnRecordChangedEvtArgs>? OnRecordChanged;
        #endregion

        #region Constructors
        public RecordSource()=>SetID();

        public RecordSource(IEnumerable<M> source) : base(source)
        {
            SetID();
            try
            {
                Current = source.First();
            }
            catch
            {
            }
        }
        #endregion

        #region Children
        public IRecordSource MakeChild()
        {
            RecordSource<M> child = new(this);
            AddChild(child);
            return child;
        }
        public void AddChild(IRecordSource child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public IRecordSource GetChild(long id)=> Children.First(s=> s.SourceID==id);

        public void ReorderChildren()
        {
            foreach (IRecordSource child in Children) child.Reorder();
        }
        public void RefilterChildren()
        {
            foreach (IRecordSource child in Children) child.Refilter();            
        }

        public void RequeryChildren()
        {
            foreach(IRecordSource child in Children) child.Requery();            
        }
        #endregion


        #region Linq
        public IEnumerable<TS> Where<TS>(Func<TS, bool> predicate,bool replace=true) where TS : IAbstractModel
        {
            var result = this.Cast<TS>().Where(predicate);
            if (replace)
            {
                var list = result.ToList();
                ReplaceData(list);
            }
            return result;
        }

        public bool Any<TS>(Func<TS, bool> predicate) where TS : IAbstractModel=>
         this.Cast<TS>().Any(predicate);

        public bool Any() => this.Cast<IAbstractModel>().Any();

        public IEnumerable<TR> Select<TS,TR>(Func<TS, TR> predicate) where TS : IAbstractModel
        {
            var result = this.Cast<TS>().Select(predicate);
            return result.ToList();
        }

        public IOrderedEnumerable<TS> OrderMe<TS, K>(Func<TS, K> predicate, SourceOrder order = SourceOrder.ASC, bool replace = true) where TS : IAbstractModel
        {           
            var result = (order.Equals(SourceOrder.ASC)) ? this.Cast<TS>().OrderBy(predicate) : this.Cast<TS>().OrderByDescending(predicate);
            var list = result.ToList();
            if (replace) ReplaceData(list);
            return result;
        }
        #endregion

        public IRecordSource Copy() => new RecordSource<M>(this);

        public object Get(int index) => this[index];
        public void SetFilter(IRecordsOrganizer recordsOrganizer) 
        {
            recordsOrganizer.SourceID = SourceID;
            Filter = recordsOrganizer;
        }

        #region CurrentRecord
        public IAbstractModel CurrentRecord() => Current;
        public void SetCurrentRecord(IAbstractModel? record)=>Current = (record==null) ? new() : (M)record;
        #endregion

        #region RequeryOptions
        public void Reorder() => Filter?.OnReorder(this);
        public void Refilter() => Filter?.OnFilter(this);
        public void Requery()
        {
            Refilter();
            Reorder();
        }
        #endregion

        #region CRUD
        public void AddRecord(IAbstractModel? record)
        {
            if (record == null || Any<M>(s=>s.IsEqualTo(record))) return;
            
            M _record = (M)record;
            bool ToAdd = true;

            if (Filter != null) ToAdd=Filter.FilterCriteria(_record);

            if (ToAdd)
            {
                Add(_record);
                Current = _record;
            }

            Filter?.OnReorder(this);
            OnRecordChanged?.Invoke(this, new(Current, RecordAction.ADD));
            foreach (var child in Children) child.AddRecord(record);            
        }

        public void RemoveRecord(IAbstractModel? record)
        {
            if (record == null) return;            
            M _record = (M)record;
            int index = IndexOf(_record);
            if (index == -1) return;
            Remove(_record);
            Current = (IsEmpty) ? new() : this[(index == 0) ? 0 : index - 1];
            OnRecordChanged?.Invoke(this, new(_record, RecordAction.DELETE));
            foreach (var child in Children) child.RemoveRecord(record);
        }

        public void UpdateRecord(IAbstractModel? record)
        {
            if (record == null) return;
            M _record = (M)record;
            int index = IndexOf(_record);
            if (index == -1) return;
            this[index] = _record;
            Current = _record;
            OnRecordChanged?.Invoke(this,new(Current,RecordAction.UPDATE));
        }

        public void ReplaceData(IEnumerable records)
        {
            IEnumerable<M> source = records.Cast<M>().ToList();
            ReplaceRange(source);
            Current = (RecordCount > 0) ? this[0] : new();
        }
        #endregion

        #region Movement
        private bool MovementAllowed(RecordMovement movement)
        {
            RecordMovingArgs = new(Current, movement);
            OnRecordMoving?.Invoke(this, RecordMovingArgs);
            return !RecordMovingArgs.Cancel;
        }
        private void UpdateRecordsPositionDisplayer()
        {
            RecordsPositionDisplayer = $"Record {CurrentPosition + 1} of {RecordCount}";
            if (IsNewRecord) RecordsPositionDisplayer = "New Record";
            OnPropertyChanged(new (nameof(RecordsPositionDisplayer)));
    }
        IEnumerator<IAbstractModel> IEnumerable<IAbstractModel>.GetEnumerator()
        {
            foreach (IAbstractModel records in this)
            {
                yield return records;
            }
        }
        public bool GoPrevious()
        {
            if (RecordCount == 0 || IsBOF) return false;
            if (IsNewRecord) return GoLast();
            if (!MovementAllowed(RecordMovement.PREVIOUS)) return false;
            Current = this[CurrentPosition - 1];
            OnRecordMoved?.Invoke(this, new(Current, RecordMovement.PREVIOUS));
            return true;
        }
        public bool GoNewRecord()
        {
            if (!CanGoNew) return false;
            if (!MovementAllowed(RecordMovement.NEW)) return false;
            Current = new();
            Add(Current);
            OnRecordMoved?.Invoke(this, new(Current, RecordMovement.NEW));
            return true;
        }
        public bool GoNext()
        {
            if (!CanGoNext && !IsEOF) return false;
            if (IsEOF) return GoNewRecord();
            if (!MovementAllowed(RecordMovement.NEXT)) return false;
            Current = this[CurrentPosition+1];
            OnRecordMoved?.Invoke(this, new(Current, RecordMovement.NEXT));
            return true;
        }
        public bool GoFirst()
        {
            if (RecordCount == 0 || !MovementAllowed(RecordMovement.FIRST)) return false;
            if (Current.IsNewRecord) Remove(Current);
            Current = this[0];
            OnRecordMoved?.Invoke(this, new(Current, RecordMovement.FIRST));
            return true;
        }
        public bool GoLast()
        {
            if (RecordCount == 0 || !MovementAllowed(RecordMovement.LAST)) return false;
            if (Current.IsNewRecord) Remove(Current);
            Current = this[LastIndex];
            OnRecordMoved?.Invoke(this, new(Current, RecordMovement.LAST));
            return true;
        }
        #endregion

        #region Reporting
        public void PrintRecords()
        {
            Console.WriteLine($"From: {ToString()} {{");
            Console.WriteLine();
            int id = 0;

            foreach (IAbstractModel record in this) Console.WriteLine($"\t {++id}) {record}");

            Console.WriteLine();
            Console.WriteLine($"}}");
        }

        public void PrintMe()=> Console.WriteLine(ToString());
        public void PrintReportStatus() => Console.WriteLine(ReportStatus());
        public void PrintCurrentRecordStatus() => Console.WriteLine(ReportCurrentRecordInfo());
        public string ReportCurrentRecordInfo()
        {
            string name = ToString();
            sb.Clear();
            sb.Append($"{name}:\n");
            if (Current.IsNewRecord)
            {
                return "New Record";
            }
            sb.Append($"{Current} - ");
            sb.Append($"Records: {CurrentPosition + 1} of {RecordCount}");
            return sb.ToString();
        }
        public string ReportStatus()
        {
            string name = ToString();
            sb.Clear();
            sb.Append($"{name}:\n");
            sb.Append($"Empty: {IsEmpty}\n");
            sb.Append($"Record Count: {RecordCount}\n");
            sb.Append((Current == null) ? "Current Record: null\n" : $"Current Record: {Current}\n");
            sb.Append($"Record Position: {CurrentPosition}\n");
            sb.Append($"New Record: {IsNewRecord}\n");
            sb.Append($"BOF: {IsBOF}\n");
            sb.Append($"IsEOF: {IsEOF}\n");
            sb.Append($"HasMoved: {HasMoved}\n");
            return sb.ToString();
        }
        #endregion

        #region Equal,ToString,HashCode
        void SetID()
        {
            ++Id;
            _sourceID = Id;
        }
        public override string ToString()
        {
            sb.Clear();
            sb.Append($"RecordSource<{typeof(M).Name}> ID: {_sourceID}");
            if (IsChild) sb.Append($" child of {Parent}");
            return sb.ToString();
        }
        public override bool Equals(object? obj) => obj is RecordSource<M> source && _sourceID == source._sourceID;
        public override int GetHashCode() => HashCode.Combine(_sourceID);
        #endregion
    }

    public static class ExtensionRecordSource
    {
        public static IRecordSource ToIRecordSource<M>(this IEnumerable source) where M : AbstractModel, new()=>
        new RecordSource<M>(source.Cast<M>());
    }
}
