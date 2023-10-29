using System.ComponentModel;

namespace SARModel
{
    public interface IAbstractController : IAbstractNotifier
    {
        public List<IAbstractController> SubControllers { get; }
        public abstract IDB DB { get; }
        public IRecordSource ChildSource { get; set; }
        public IRecordSource MainSource { get; }
        public IAbstractModel? SelectedRecord { get; set; }
        public Type ModelType { get; }

        /// <summary>
        /// This property manages the Form Progress Bar. If True the bar is running.<br/>
        /// When running long tasks, you might need to use <c>await Task.Run(function)</c>. The function should return False or Task&lt;false><br/>
        /// to stop the Progress Bar from keep running.
        /// <para>For Example:</para>
        /// <code>
        /// IsLoading = true; &lt;--- Progress Bar starts running
        /// IsLoading = await Task.Run(aFunction); &lt;--- Progress Bar stops running when the funcion completes.
        /// </code>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        public bool IsLoading { get; set; }

        /// <summary>
        /// This property tells if any record in the RecordSource has had changes and was not yet saved.
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        public bool IsDirty { get; }
        public abstract void AllowNewRecord(bool value);

        /// <summary>
        /// This is a property to be used for simple searching criteria
        /// <para>For Example:</para>
        /// Write the following code in the OnAfterUpdate method/>
        /// <include file='Docs.xml' path='docs/simpleSearchExample'/>
        /// <include file='Docs.xml' path='docs/searchFilter'/>
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        public string Search { get; set; }

        #region Movements
        /// <summary>
        /// <para>
        /// This method is used in conjunction with OpenRecord, OpenNewRecord and RecordMoved.
        /// </para>
        ///It works as a bridge between the Form and the RecordSource
        ///and tells the RecordSource how to filter the results and allows 
        ///the Form to reflect those results
        /// <para>
        /// If used with OpenRecord and/or OpenNewRecord, it must be decleared in the Page/Window's constructor Form as shown below
        /// </para>
        /// <code>
        ///  public WindowForm(IAbstractModel record) : this()=>
        ///  Controller.OnAppearingGoTo(record);
        /// </code>
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        public void OnAppearingGoTo(IAbstractModel? record);

        /// <summary>
        /// It moves to a given Record
        /// </summary>
        /// <include file='Docs.xml' path='docs/author'/>
        /// <param name="record"></param>
        public void GoTo(IAbstractModel record);
        #endregion

        #region UI
        public bool UIIsWindow();
        public bool UIIsPage();
        public void SetUI(object obj);
        public T? GetUI<T>();
        #endregion

        #region CRUD
        public abstract bool Save(IAbstractModel? record);
        public abstract bool Delete(IAbstractModel? record);

        /// <summary>
        /// This method undos all the changes NOT YET saved in the RecordSource 
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        public void UndoChanges();

        /// <summary>
        /// This saves all the changes applied to records in the RecordSource 
        /// <para></para>
        /// <include file="Docs.xml" path="docs/isDirtyProp"/>
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        public void CommitChanges();
        #endregion

        #region OpenCloseForms
        /// <summary>
        /// Open a Window displaying the given record
        /// <para>For Example:</para>
        /// <code>
        /// Window window = new(record);
        /// window.ShowDialog();
        /// </code>
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public abstract void OpenRecord(IAbstractModel record);
        public abstract void OpenNewRecord(IAbstractModel record);

        /// <summary>
        /// Run a series of checks before closing the Form
        /// <para>For Example:</para>
        /// <para>
        /// In your Page or Window UI override the OnClosing(e) method
        /// </para>
        /// <para>
        /// </para>
        /// Then add this code inside the overridden method:
        /// <code>
        /// base.OnClosing(e);
        /// Controller.OnFormClosing(e);
        /// </code>
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        /// <returns>A boolean that tells if the closing occured or no</returns>
        public bool OnFormClosing(CancelEventArgs e);
        #endregion

        #region Dialogs
        public bool DeleteActionConfirmed();
        public bool SaveActionConfirmed();
        #endregion

        /// <summary>
        /// This method check if a record meets the mandatory criteria to be saved.
        /// <para>
        /// If the record is Dirty the system will prompt the user asking if they wish to save the changes
        /// </para>
        /// <include file="Docs.xml" path="docs/isDirtyProp"/>
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        /// <returns>True if the record has met the creteria. If False Save/Update methods will be stopped together with Movement methods</returns>
        public bool RecordIntegrityCheck();

        /// <summary>
        /// <para>Usefull method to run a simple research in the Form's SearchBar.</para>
        /// <para>For Example:</para>
        /// Write the following code in the OnAfterUpdate() method
        /// <include file='Docs.xml' path='docs/simpleSearchExample'/>
        /// <include file='Docs.xml' path='docs/searchProp'/>
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        public static virtual bool SearchFilter(object? record, string? criteria) => throw new NotImplementedException();

        public static Task<Excel> CallExcel(OfficeFileMode mode, string path) => throw new NotImplementedException();

        /// <summary>
        /// When overriden, you can implement your own logic to deal with Office Applications.<br/>
        /// You might need to add this to the Tab Control:
        /// <code>
        /// private void ExcelMenuItemClick(object sender, RoutedEventArgs e) =>
        /// Tab.GetTabView().Controller.RunOffice(OfficeApplication.EXCEL);
        /// </code>
        /// Also override this method in your controller and implement a logic:<br/>
        /// For Example:
        /// <code>
        /// IsLoading = true;
        /// IsLoading = await Task.Run(() =>
        ///     WriteExcel(fileMode, path, (excel) =>
        ///     { 
        ///       //Styles here
        ///     }));
        /// </code>
        /// <include file='Docs.xml' path='docs/author'/><br/>
        /// See the <see cref="TableRange.Style(string, Style)"/> method.
        /// </summary>
        /// <param name="officeApp">Enum</param>
        public abstract void RunOffice(OfficeApplication officeApp);

        /// <summary>
        /// This Task organises the data to be printed onto a spreadsheet into a 2D object Array.<br/>
        /// For Example:
        /// <code>
        /// object?[,] data = GenerateDataTable("COL1", "COL2", ... ,"COLN"),
        /// Parallel.For(0, ChildSource.RecordCount, (row) =>
        /// {
        ///     Model record = (Model)ChildSource.Get(row);
        ///     int r = row + 1;
        ///     data[r, 0] = record.Field1;
        ///     data[r, 1] = record.Field2;
        ///     ...
        ///     data[r, n] = record.FieldN;
        /// });
        /// return Task.FromResult(data);
        /// </code>
        /// <include file='Docs.xml' path='docs/author'/>
        /// </summary>
        /// <returns>A 2D object Array Task</returns>
        public abstract Task<object?[,]> OrganiseExcelData();
    }
}
