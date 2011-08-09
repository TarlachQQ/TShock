
/*   
TShock, a server mod for Terraria
Copyright (C) 2011 The TShock Team

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using DataContext = DbLinq.Data.Linq.DataContext;

namespace TShockAPI.DB
{
    public class UserManager
    {
        private DataContext database;
        public DbLinq.Data.Linq.Table<User> Users { get; protected set; }

        public UserManager(DataContext db)
        {
            database = db;
            Users = database.GetTable<User>();

            var table = new SqlTable("Users",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Username", MySqlDbType.VarChar, 32) { Unique = true },
                new SqlColumn("Password", MySqlDbType.VarChar, 128),
                new SqlColumn("Usergroup", MySqlDbType.Text),
                new SqlColumn("IP", MySqlDbType.VarChar, 16)
            );
            var creator = new SqlTableCreator(db.Connection, db.Connection.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureExists(table);

            String file = Path.Combine(TShock.SavePath, "users.txt");
            if (File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    String line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Equals("") || line.Substring(0, 1).Equals("#"))
                            continue;
                        String[] info = line.Split(' ');
                        String username = "";
                        String sha = "";
                        String group = "";
                        String ip = "";

                        String[] nameSha = info[0].Split(':');

                        if (nameSha.Length < 2)
                        {
                            username = nameSha[0];
                            ip = nameSha[0];
                            group = info[1];
                        }
                        else
                        {
                            username = nameSha[0];
                            sha = nameSha[1];
                            group = info[1];
                        }

                        AddUser(new User(ip.Trim(), username.Trim(), sha.Trim(), group.Trim()));
                    }
                }
                String path = Path.Combine(TShock.SavePath, "old_configs");
                String file2 = Path.Combine(path, "users.txt");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                if (File.Exists(file2))
                    File.Delete(file2);
                File.Move(file, file2);
            }

        }

        /// <summary>
        /// Adds a given username to the database
        /// </summary>
        /// <param name="user">User user</param>
        public void AddUser(User user)
        {
            try
            {
                if (!TShock.Groups.GroupExists(user.Group))
                    throw new GroupNotExistsException(user.Group);

                Users.InsertOnSubmit(user);
                database.SubmitChanges(ConflictMode.ContinueOnConflict);
            }
            catch (Exception ex)
            {
                throw new UserManagerException("AddUser SQL returned an error", ex);
            }
        }

        /// <summary>
        /// Removes a given username from the database
        /// </summary>
        /// <param name="user">User user</param>
        public void RemoveUser(User user)
        {
            try
            {
                var gotuser = GetUser(user);
                if (gotuser == null)
                    throw new UserNotExistException(string.IsNullOrEmpty(user.Address) ? user.Name : user.Address);

                Users.DeleteOnSubmit(gotuser);
                database.SubmitChanges();
            }
            catch (Exception ex)
            {
                throw new UserManagerException("RemoveUser SQL returned an error", ex);
            }
        }



        public void UpdateUsers()
        {
            try
            {
                database.SubmitChanges();
            }
            catch (Exception ex)
            {
                throw new UserManagerException("UpdateUser SQL returned an error", ex);
            }
        }

        /// <summary>
        /// Returns a Group for a ip from the database
        /// </summary>
        /// <param name="ply">string ip</param>
        public Group GetGroupForIP(string ip)
        {
            try
            {
                var user = (from u in Users where u.Address == ip select u).FirstOrDefault();
                if (user != null)
                {
                    return Tools.GetGroup(user.Group);
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError("GetGroupForIP SQL returned an error: " + ex);
            }
            return Tools.GetGroup("default");
        }

        public Group GetGroupForIPExpensive(string ip)
        {
            try
            {
                var users = from u in Users select u;
                foreach (var user in users)
                {
                    if (Tools.GetIPv4Address(user.Address) == ip)
                    {
                        return Tools.GetGroup(user.Group);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError("GetGroupForIP SQL returned an error: " + ex);
            }
            return Tools.GetGroup("default");
        }


        public User GetUserByName(string name)
        {
            try
            {
                return GetUser(new User { Name = name });
            }
            catch (UserManagerException)
            {
                return null;
            }
        }
        public User GetUserByIP(string ip)
        {
            try
            {
                return GetUser(new User { Address = ip });
            }
            catch (UserManagerException)
            {
                return null;
            }
        }
        public User GetUser(User user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.Address))
                    return (from u in Users where user.Name == u.Name select u).FirstOrDefault();

                return (from u in Users where user.Address == u.Address select u).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new UserManagerException("GetUserID SQL returned an error", ex);
            }
        }
    }

    [Table(Name = "Users")]
    public class User
    {
        [Column(Name = "ID", DbType = "INTEGER", IsPrimaryKey = true, CanBeNull = false)]
        public int ID { get; set; }
        [Column(Name = "Username", DbType = "TEXT")]
        public string Name { get; set; }
        [Column(Name = "Password", DbType = "TEXT")]
        public string Password { get; set; }
        [Column(Name = "Usergroup", DbType = "TEXT")]
        public string Group { get; set; }
        [Column(Name = "IP", DbType = "TEXT")]
        public string Address { get; set; }

        public User(string ip, string name, string pass, string group)
        {
            Address = ip;
            Name = name;
            Password = pass;
            Group = group;
        }
        public User()
        {
            Address = "";
            Name = "";
            Password = "";
            Group = "";
        }
    }

    [Serializable]
    public class UserManagerException : Exception
    {
        public UserManagerException(string message)
            : base(message)
        {

        }
        public UserManagerException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
    [Serializable]
    public class UserExistsException : UserManagerException
    {
        public UserExistsException(string name)
            : base("User '" + name + "' already exists")
        {
        }
    }
    [Serializable]
    public class UserNotExistException : UserManagerException
    {
        public UserNotExistException(string name)
            : base("User '" + name + "' does not exist")
        {
        }
    }
    [Serializable]
    public class GroupNotExistsException : UserManagerException
    {
        public GroupNotExistsException(string group)
            : base("Group '" + group + "' does not exist")
        {
        }
    }
}
