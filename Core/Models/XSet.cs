using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace Uniya.Core;

// ----------------------------------------------------------------------------------------
#region ** XSet collection interface

public interface ISetCollection
{
    /// <summary>Get table schema of this collection.</summary>
    ITableSchema Schema { get; }
    /// <summary>Gets created or modified entities.</summary>
    /// <param name="state">The entity state.</param>
    /// <returns>The created or modified list.</returns>
    ICollection<XEntity> GetEntities(XEntityState state);
    /// <summary>Gets deleted entities with clear deleted list.</summary>
    /// <returns>The deleted list.</returns>
    ICollection<XEntity> GetDeletedWithClear();
}

#endregion

/// <summary>
/// Proxy of database.
/// </summary>
public class XSetCollection<T> : ObservableCollection<T>, ISetCollection where T : class
{
    // ------------------------------------------------------------------------------------
    #region ** dynamic object model

    private Dictionary<long, int> _cache = new Dictionary<long, int>();
    private List<T> _deleted = new List<T>();

    /// <summary>
    /// Gets object by identifier.
    /// </summary>
    /// <param name="id">The object identifier.</param>
    /// <returns>Really object if found, otherwise <b>null</b>.</returns>
    public T GetById(long id)
    {
        if (_cache.ContainsKey(id))
        {
            int idx = _cache[id];
            if (idx >= 0 && idx < Count)
                return this[idx];
        }
        XSet.Trace($"Not found a object for ID={id}", 'w');
        return default(T);
    }

    // ** support ISetCollection

    /// <summary>Get table schema of this collection.</summary>
    public ITableSchema Schema { get; private set; }

    /// <summary>
    /// Read some entities using parameters.
    /// </summary>
    /// <param name="data">The read-only database interface.</param>
    /// <param name="pairs">The pair of column name and value.</param>
    /// <returns>Without information.</returns>
    //public async Task Read(IReadonlyData data, params KeyValuePair<string, object>[] pairs)
    //{
    //    _cache.Clear();
    //    var entityName = typeof(T).Name.Substring(1);
    //    foreach (var table in XSet.Schema.Tables)
    //    {
    //        if (table.TableName.Equals(entityName))
    //        {
    //            foreach (var entity in await data.Read(entityName, pairs))
    //            {
    //                if (entity.Actualization(table))
    //                    Add(entity.To<T>());
    //            }
    //            break;
    //        }
    //    }
    //}
    /// <summary>Gets created or modified entities.</summary>
    /// <param name="state">The entity state.</param>
    /// <returns>The created or modified collection.</returns>
    public ICollection<XEntity> GetEntities(XEntityState state)
    {
        var collection = new Collection<XEntity>();
        if (state == XEntityState.Created)
        {
            var now = DateTime.Now;
            foreach (var item in this)
            {
                var db = item as IDB;
                db.Created = now;
                db.Modified = now;
                collection.Add(XEntity.From(item));
            }
        }
        return collection;
    }
    /// <summary>Gets deleted entities with clear deleted list.</summary>
    /// <returns>The deleted collection.</returns>
    public ICollection<XEntity> GetDeletedWithClear()
    {
        var collection = new Collection<XEntity>();
        foreach (var item in _deleted)
        {
            collection.Add(XEntity.From(item));
        }
        _deleted.Clear();
        return collection;
    }
    /// <summary>Actualization all changes.</summary>
    //public void Actualization()
    //{
    //    foreach (var item in this)
    //    {
    //        var proxy = item as XProxy;
    //        if (proxy != null)
    //            proxy.Entity.Actualization();
    //    }
    //    _deleted.Clear();
    //}

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** override object model

    /// <summary>Removes all items from the collection.</summary>
    protected override void ClearItems()
    {
        foreach (var id in _cache.Keys)
        {
            int idx = _cache[id];
            if (idx >= 0 && idx < Count)
            {
                _deleted.Add(this[idx]);
            }
        }
        base.ClearItems();
        _cache.Clear();
    }
    /// <summary>Inserts an item into the collection at the specified index.</summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The object to insert.</param>
    protected override void InsertItem(int index, T item)
    {
        var db = item as IDB;
        var id = db.Id;
        if (_cache.ContainsKey(id))
        {
            XSet.Trace($"Already exist in collection ID={id}", 'w');
            return;
        }
        base.InsertItem(index, item);
        _cache.Add(id, index);
    }
    /// <summary>Moves the item at the specified index to a new location in the collection.</summary>
    /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
    /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
    protected override void MoveItem(int oldIndex, int newIndex)
    {
        var db = this[oldIndex] as IDB;
        base.MoveItem(oldIndex, newIndex);
        _cache[db.Id] = newIndex;
    }

    /// <summary>Removes the item at the specified index of the collection.</summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    protected override void RemoveItem(int index)
    {
        var item = this[index];
        var db = item as IDB;
        _cache.Remove(db.Id);
        base.RemoveItem(index);
        _deleted.Add(item);
    }
    /// <summary>Replaces the element at the specified index.</summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="item">The new value for the element at the specified index.</param>
    protected override void SetItem(int index, T item)
    {
        var db = item as IDB;
        var id = db.Id;

        if (_cache.ContainsKey(id))
        {
            XSet.Trace($"Already exist in collection ID={id}", 'w');
            return;
        }
        _cache.Add(id, index);
        base.SetItem(index, item);
    }

    #endregion
}

/// <summary>
/// Proxy of database.
/// </summary>
public class XSet : IEntitySet
{
    // ------------------------------------------------------------------------------------
    #region ** general object model

    private List<INotifyCollectionChanged> _list = new List<INotifyCollectionChanged>();
    public static ISchema Schema = XConnector.GetSchema();

    public XSet()
    {
        // base
        AddSet(Persons = new XSetCollection<IPerson>());
        AddSet(Users = new XSetCollection<IUser>());
        AddSet(Roles = new XSetCollection<IRole>());
        AddSet(UserRoles = new XSetCollection<IUserRole>());
        AddSet(UserSessions = new XSetCollection<IUserSession>());
        AddSet(Connections = new XSetCollection<IConnection>());
        AddSet(Tasks = new XSetCollection<ITask>());
        AddSet(Parameters = new XSetCollection<IParameter>());
        AddSet(Scripts = new XSetCollection<IScript>());
    }

    void AddSet(INotifyCollectionChanged notify)
    {
        _list.Add(notify);
        notify.CollectionChanged += OnCollectionChanged;
    }

    // base
    public XSetCollection<IPerson> Persons { get; private set; }
    public XSetCollection<IUser> Users { get; private set; }
    public XSetCollection<IRole> Roles { get; private set; }
    public XSetCollection<IUserRole> UserRoles { get; private set; }
    public XSetCollection<IUserSession> UserSessions { get; private set; }

    public XSetCollection<IConnection> Connections { get; private set; }
    public XSetCollection<ITask> Tasks { get; private set; }

    public XSetCollection<IParameter> Parameters { get; private set; }
    public XSetCollection<IScript> Scripts { get; private set; }

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** IEntitySet implementation

    /// <summary>Creating entities.</summary>
    public ICollection<XEntity> Creating
    {
        get { return GetEntities(XEntityState.Created); }
    }
    /// <summary>Updating entities.</summary>
    public ICollection<XEntity> Updating
    {
        get { return GetEntities(XEntityState.Modified); }
    }
    /// <summary>Deleting entities with clear deleted lists.</summary>
    public ICollection<XEntity> Deleting
    {
        get
        {
            var list = new Collection<XEntity>();
            for (int i = _list.Count; i > 0; i--)
            {
#pragma warning disable IDE0019
                ISetCollection collection = _list[i - 1] as ISetCollection;
#pragma warning restore IDE0019
                if (collection != null)
                {
                    foreach (var entity in collection.GetDeletedWithClear())
                        list.Add(entity);
                }
            }
            return list;
        }
    }
    ICollection<XEntity> GetEntities(XEntityState state)
    {
        var list = new Collection<XEntity>();
        for (int i = 0; i < _list.Count; i++)
        {
#pragma warning disable IDE0019
            ISetCollection collection = _list[i] as ISetCollection;
#pragma warning restore IDE0019
            if (collection != null)
            {
                foreach (var entity in collection.GetEntities(state))
                    list.Add(entity);
            }
        }
        return list;
    }
    #endregion

    // ------------------------------------------------------------------------------------
    #region ** dynamic object model

    //public async void Select(ICrudData data, params XQuery[] queries)
    //{
    //    foreach (var query in queries)
    //    {
    //        var collection = await data.Select(query);
    //        foreach (var entity in collection)
    //        {

    //        }
    //    }
    //}

    //public async Task Load(ICrudData data)
    //{
    //    for (int i = 0; i < _list.Count; i++)
    //    {
    //        var collection = _list[i] as ISetCollection;
    //        if (collection != null)
    //        {
    //            //data.Read()
    //            //var query = collection.GetQuery(null);
    //            //foreach (var entity in await data.Select(query))
    //            //{

    //            //}
    //            //data.Read()
    //            //collection.GetEntities
    //            //foreach (var entity in collection.GetEntities(state))
    //            //    list.Add(entity);
    //        }
    //    }
    //}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public async System.Threading.Tasks.Task CommitChanges(ITransactedData data)
    {
        await data.Transaction(this);
    }

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** implement object model

    //private XProxy GetProxy(XEntity entity)
    //{
    //    switch (entity.EntityName)
    //    {
    //        case "User":
    //            //return new XProxy(entity);
    //            break;
    //    }
    //    return new XProxy(entity);
    //}

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        string oldText, newText;
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                newText = (e.NewItems[0] != null) ? e.NewItems[0].ToString() : "NULL";
                Trace($"Добавлен новый объект: {newText}");
                break;
            case NotifyCollectionChangedAction.Remove:
                oldText = (e.OldItems[0] != null) ? e.OldItems[0].ToString() : "NULL";
                Trace($"Удален объект: {oldText}");
                break;
            case NotifyCollectionChangedAction.Replace:
                newText = (e.NewItems[0] != null) ? e.NewItems[0].ToString() : "NULL";
                oldText = (e.OldItems[0] != null) ? e.OldItems[0].ToString() : "NULL";
                Trace($"Объект {oldText} заменен объектом {newText}");
                break;
        }
    }

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** fill primary database

    public void Fill()
    {
        // --------------------------------------------------------------
        IPerson person;

        var dtNow = DateTime.Now;

        person = XProxy.Get<IPerson>();
        person.Id = 1;
        person.FirstName = "Administrator";
        person.LastName = "Unknown";
        person.Created = dtNow;
        person.Modified = dtNow;
        Persons.Add(person);

        // --------------------------------------------------------------
        IUser user;

        XProxy.TestPassword("Passw0rd", out string hash, out string salt);

        user = XProxy.Get<IUser>();
        user.Id = 1;
        user.Name = "Admin";
        user.PsswdHash = hash;
        user.PsswdSalt = salt;
        user.Email = "admin@triotour.com";
        user.IsEmailChecked = true;
        user.Phone = "+7(910)5540021";
        user.IsPhoneChecked = true;
        user.IsActive = true;
        user.PersonId = 1;
        user.Created = dtNow;
        user.Modified = dtNow;
        Users.Add(user);

        // --------------------------------------------------------------
        IRole role;

        role = XProxy.Get<IRole>();
        role.Id = 1;
        role.Name = XRole.Reader;
        role.Title = "Readers";
        role.Description = "Minimal rights for read only";
        role.IsActive = true;
        role.Created = dtNow;
        role.CreatedUserId = 1;
        role.Modified = dtNow;
        role.ModifiedUserId = 1;
        Roles.Add(role);

        role = XProxy.Get<IRole>();
        role.Id = 2;
        role.Name = XRole.Writer;
        role.Title = "Writers";
        role.Description = "Optimal rights for create, update or delete";
        role.IsActive = true;
        role.ParentId = 1;
        role.Created = dtNow;
        role.CreatedUserId = 1;
        role.Modified = dtNow;
        role.ModifiedUserId = 1;
        Roles.Add(role);

        role = XProxy.Get<IRole>();
        role.Id = 3;
        role.Name = XRole.Administrator;
        role.Title = "Administrators";
        role.Description = "Full rights";
        role.IsActive = true;
        role.ParentId = 2;
        role.Created = dtNow;
        role.CreatedUserId = 1;
        role.Modified = dtNow;
        role.ModifiedUserId = 1;
        Roles.Add(role);

        // --------------------------------------------------------------
        IUserRole userRole;

        userRole = XProxy.Get<IUserRole>();
        userRole.Id = 1;
        userRole.UserId = 1;
        userRole.RoleId = 3;
        userRole.IsActive = true;
        userRole.Created = dtNow;
        userRole.CreatedUserId = 1;
        userRole.Modified = dtNow;
        userRole.ModifiedUserId = 1;
        userRole.Note = "Default user's role";
        UserRoles.Add(userRole);

        // --------------------------------------------------------------
        IConnection connection;

        connection = XProxy.Get<IConnection>();
        connection.Id = 1;
        connection.Name = "base";
        connection.Title = "Base";
        connection.Description = "Default SQLite connection";
        connection.HostUrl = "https://api.triotour.com/";
        connection.ClassName = "Uniya.Connectors.Sqlite.SqliteConnector";
        connection.ComplexCode = XProxy.Encrypt(@"Data Source = (LocalDB)\mssqllocaldb; Initial Catalog = master; Integrated Security = True;");
        connection.IsActive = true;
        connection.Created = dtNow;
        connection.CreatedUserId = 1;
        connection.Modified = dtNow;
        connection.ModifiedUserId = 1;
        Connections.Add(connection);

        connection = XProxy.Get<IConnection>();
        connection.Id = 2;
        connection.Name = "trio_tour";
        connection.Title = "triotour.com";
        connection.Description = "Company TrioTour MS SQL connection";
        connection.HostUrl = "https://api.triotour.com/api/data/trio_tour";
        connection.ClassName = "Uniya.Connectors.MsSql.MsSqlConnector";
        connection.ComplexCode = XProxy.Encrypt("data source=ms-sql-7.in-solve.ru;initial catalog=1gb_uniya2;User ID=1gb_triotour;Password=b7ac0e623rty;");
        connection.IsActive = false;
        connection.Created = dtNow;
        connection.CreatedUserId = 1;
        connection.Modified = dtNow;
        connection.ModifiedUserId = 1;
        Connections.Add(connection);

        connection = XProxy.Get<IConnection>();
        connection.Id = 3;
        connection.Name = "practic_share_point";
        connection.Title = "SharePoint Register";
        connection.Description = "SharePoint Register connection";
        connection.HostUserName = "VDoroshenko@a-practic.ru";
        connection.HostPassword = XProxy.Encrypt("Practic8");
        connection.HostUrl = "https://practic.sharepoint.com/sites/register";
        connection.ClassName = "Uniya.Connectors.SharePoint.SharePointConnector";
        connection.ComplexCode = XProxy.Encrypt("VDoroshenko@a-practic.ru|Practic8");
        connection.IsActive = true;
        connection.Created = dtNow;
        connection.CreatedUserId = 1;
        connection.Modified = dtNow;
        connection.ModifiedUserId = 1;
        Connections.Add(connection);

        connection = XProxy.Get<IConnection>();
        connection.Id = 4;
        connection.Name = "finguru_bitrix24";
        connection.Title = "SharePoint Register";
        connection.Description = "SharePoint Register connection";
        connection.HostUrl = "https://portal.finguru.com/api";
        connection.ClassName = "Uniya.Connectors.Bitrix24.Bitrix24Connector";
        connection.ComplexCode = XProxy.Encrypt("local.57fe05cb295182.55185886|6MTR5Go2xRVjWcHfTlKhvM5AshunytE9Ml739omM66z92jwVHv");
        connection.IsActive = false;
        connection.Created = dtNow;
        connection.CreatedUserId = 1;
        connection.Modified = dtNow;
        connection.ModifiedUserId = 1;
        Connections.Add(connection);

        // --------------------------------------------------------------
        ITask task;

        task = XProxy.Get<ITask>();
        task.Id = 1;
        task.Name = "Backup";
        task.Title = "Backup";
        task.Description = "Backup base storage";
        task.ClassName = "Uniya.Tasks.Backup";
        task.IsActive = false;
        task.Created = dtNow;
        task.CreatedUserId = 1;
        task.Modified = dtNow;
        task.ModifiedUserId = 1;
        task.Note = "Backup";
        Tasks.Add(task);

        // --------------------------------------------------------------
        IParameter parameter;

        parameter = XProxy.Get<IParameter>();
        parameter.Id = 1;
        parameter.Name = "LastRegistryDate";
        parameter.Title = "Registry date";
        parameter.Description = "Last date and time of change registry";
        parameter.Created = dtNow;
        parameter.CreatedUserId = 1;
        parameter.Modified = dtNow;
        parameter.ModifiedUserId = 1;
        parameter.Value = "2019-08-15T17:00:00";
        parameter.Note = "Backup";
        Parameters.Add(parameter);


        //INSERT INTO[Person] ([Id], [FirstName], [SecondName], [LastName], [Birthday], [Phone], [Email], [Created], [Modified], [Note])
        //AddPerson(1, "Admin", "", "Unknown", "1970-04-26T09:00:01", "", "", "2019-08-15T17:00:02", "2019-08-15T17:00:02", "Fake person");

        //INSERT INTO[User] ([Id], [Name], [Active], [Password], [PersonId], [Created], [Modified], [Title], [Role], [Note])
        //AddUser(1, "admin", true, "nimda", 1, "2019-08-15T17:00:02", "2019-08-15T17:00:02", "Administrator", "admin", "Default administrator");

        //INSERT INTO[Role] ([Id], [Name], [Active], [Created], [Modified], [Title], [Note])
        //AddRole(1, "admin", true, "2019-08-15T17:00:02", "2019-08-15T17:00:02", "Administrators", "Full rights");
        //AddRole(2, "reader", true, "2019-08-15T17:00:02", "2019-08-15T17:00:02", "Readers", "Minimal rights for read only");
        //AddRole(3, "writer", true, "2019-08-15T17:00:02", "2019-08-15T17:00:02", "Writers", "Optimal rights for create, update or delete");

        //INSERT INTO[UserRole] ([Id], [Active], [UserId], [RoleId], [Created], [Modified], [Note])
        //AddUserRole(1, true, 1, 1, "2019-08-15T17:00:02", "2019-08-15T17:00:02", "Default administrator");
        //AddUserRole(2, true, 2, 2, "2019-08-15T17:00:02", "2019-08-15T17:00:02", "Any user");

        //INSERT INTO[Connection] ([Id], [Name], [Active], [ClassName], [Created], [Modified], [Title], [HostUrl], [Note])
        //AddConnection(1, "sys", true, "Uniya.Connectors.Sqlite.SqliteConnector", "2019-08-15T17:00:01", "2019-08-15T17:00:01", "Uniya System", "localhost", @"Data Source=(LocalDB)\mssqllocaldb;Initial Catalog=master;Integrated Security=True;");
        //AddConnection(2, "tt", true, "Uniya.Connectors.MsSql.MsSqlConnector", "2019-08-15T17:00:01", "2019-08-15T17:00:01", "Triotour Uniya", "api.triotour.com", "data source=ms-sql-7.in-solve.ru;initial catalog=1gb_uniya2;User ID=1gb_triotour;Password=b7ac0e623rty;");
        //AddConnection(3, "ftsp", true, "Uniya.Connectors.SharePoint.SharePointConnector", "2019-08-15T17:00:01", "2019-08-15T17:00:01", "Finguru SharePoint REG", "https://practic.sharepoint.com/sites/register", "VDoroshenko@a-practic.ru|Practic8");
        //AddConnection(4, "fgbx", true, "Uniya.Connectors.Bitrix24.Bitrix24Connector", "2021-03-10T12:30:01", "2021-03-10T12:30:01", "Finguru Bitrix24 PORTAL", "https://portal.finguru.com", "local.57fe05cb295182.55185886|6MTR5Go2xRVjWcHfTlKhvM5AshunytE9Ml739omM66z92jwVHv");
        //AddConnection(999, "test", true, "Uniya.Connectors.MsSql.MsSqlConnector", "2019-08-15T17:00:01", "2019-08-15T17:00:01", "Test Uniya", "192.168.173.50", "data source=192.168.173.50;initial catalog=TestDb1;User ID=sa;Password=1234Qwer;");

        //INSERT INTO[Task]([Id], [Name], [Active], [ClassName], [Created], [Modified], [Title], [Autostart], [Connections])
        //AddTask(1, "SysBackup", false, "Uniya.Tasks.SystemBackupTask", "2019-08-15T17:00:01", "2019-08-15T17:00:01", "Uniya System Backup MS SQL data", false, 0, "sys");
        //AddTask(999, "TestBackup", false, "Uniya.Tasks.TestBackupTask", "2019-08-15T17:00:01", "2019-08-15T17:00:01", "Uniya Test Backup MS SQL data", false, 0, "test");

        //INSERT INTO[Parameter]([Id], [Name], [Title], [ParamValue], [Created], [Modified])
        //AddParameter(1, "AccountantMonth", "Month of accountant", "201905", "2019-08-15T17:00:01", "2019-08-15T17:00:01");
        //AddParameter(2, "LastRegistryDate", "Last date in the registry", "2019-03-06T15:03:27", "2019-08-15T17:00:01", "2019-08-15T17:00:01");
        //AddParameter(3, "LastRegistryId", "Last ID in the registry", "164679", "2019-08-15T17:00:01", "2019-08-15T17:00:01");

        //dynamic userEntiny = new XEntity("User");
        //userEntiny.Id = "20";
        //userEntiny.Created = DateTime.Now;
        //userEntiny.Modified = DateTime.Now;
        //userEntiny.Name = a.Name;
        //userEntiny.Password = a.Password;
        //userEntiny.IsActive = false;

        //IUser user = userEntiny.To<IUser>();
        //Assert.Equal(user.Password, a.Password);
        //Assert.Equal(user.Name, a.Name);

        //IRole role = roleEntiny.To<IRole>();
        //Assert.Equal(role.Name, a.Role);

        //dynamic roleEntiny = new XEntity("Role");
        //roleEntiny.Id = a.Id;
        //roleEntiny.Created = DateTime.Now;
        //roleEntiny.Modified = DateTime.Now;
        //roleEntiny.Name = a.Role;
        //roleEntiny.Note = "Test role";
        //roleEntiny.CreatedUserId = 20;
        //roleEntiny.ModifiedUserId = 20;
    }
    #endregion

    // ------------------------------------------------------------------------------------
    #region ** static object model

    /// <summary>
    /// Trace diagnostic information.
    /// </summary>
    /// <param name="message">The text message about issue.</param>
    public static void Trace(string message, char level = 'i')
    {
        var dt = DateTime.Now;
        Console.WriteLine($"{dt.ToShortDateString()} {dt.ToShortTimeString()} {level}: {message}");
    }
    /// <summary>
    /// Trace diagnostic information.
    /// </summary>
    /// <param name="condition"><b>true</b> to cause a message to be written; otherwise, <b>false</b>.</param>
    /// <param name="message">The text message about issue.</param>
    public static void TraceIf(bool condition, string message)
    {
        if (condition) Trace(message, '?');
    }
    /// <summary>
    /// Trace diagnostic information.
    /// </summary>
    /// <param name="condition"><b>true</b> to cause a message to be written; otherwise, <b>false</b>.</param>
    /// <param name="message">The text message about issue.</param>
    public static void TraceThrow(bool condition, string message)
    {
        if (condition)
        {
            Trace(message, 'X');
            throw new FormatException(message);
        }
    }

    #endregion
}
