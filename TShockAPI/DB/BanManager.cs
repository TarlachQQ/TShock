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
using System.Data.Linq.Mapping;
using System.IO;
using System.Linq;
using DbLinq.Data.Linq;
using DbLinq.Sqlite;
using MySql.Data.MySqlClient;

namespace TShockAPI.DB
{
    public class BanManager
    {
        private DataContext database;
        public Table<Ban> Bans { get; protected set; }

        public BanManager(DataContext db)
        {
            database = db;
            Bans = db.GetTable<Ban>();

            var table = new SqlTable("Bans",
                new SqlColumn("IP", MySqlDbType.String, 16) { Primary = true },
                new SqlColumn("Name", MySqlDbType.Text),
                new SqlColumn("Reason", MySqlDbType.Text)
            );
            var creator = new SqlTableCreator(db.Connection, db.Connection.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureExists(table);

            String file = Path.Combine(TShock.SavePath, "bans.txt");
            if (File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    String line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        String[] info = line.Split('|');
                        AddBan(info[0].Trim(), info[1].Trim(), info[2].Trim());
                    }
                }
                String path = Path.Combine(TShock.SavePath, "old_configs");
                String file2 = Path.Combine(path, "bans.txt");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                if (File.Exists(file2))
                    File.Delete(file2);
                File.Move(file, file2);
            }
        }

        public Ban GetBanByIp(string ip)
        {
            try
            {
                return (from b in Bans where b.IP == ip select b).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return null;
        }

        public Ban GetBanByName(string name, StringComparison comparison = StringComparison.InvariantCulture)
        {
            if (!TShock.Config.EnableBanOnUsernames)
            {
                return null;
            }
            try
            {
                return (from b in Bans where name.Equals(b.Name, comparison) select b).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return null;
        }

        public bool AddBan(Ban ban)
        {
            try
            {
                Bans.InsertOnSubmit(ban);
                database.SubmitChanges();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }

        public bool AddBan(string ip, string name = "", string reason = "")
        {
            return AddBan(new Ban(ip, name, reason));
        }

        public bool RemoveBan(string ip)
        {
            try
            {
                var ban = GetBanByIp(ip);
                if (ban != null)
                {
                    Bans.DeleteOnSubmit(ban);
                    database.SubmitChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }
        public bool ClearBans()
        {
            try
            {
                return database.Connection.Query("DELETE FROM Bans") != 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }
    }

    [Table(Name = "Bans")]
    public class Ban
    {
        [Column(Name = "IP", DbType = "TEXT", IsPrimaryKey = true, IsDbGenerated = false, AutoSync = AutoSync.Never, CanBeNull = false)]
        public string IP { get; set; }
        [Column(Name = "Name", DbType = "TEXT", IsDbGenerated = false, AutoSync = AutoSync.Never)]
        public string Name { get; set; }
        [Column(Name = "Reason", DbType = "TEXT", IsDbGenerated = false, AutoSync = AutoSync.Never)]
        public string Reason { get; set; }

        public Ban(string ip, string name, string reason)
        {
            IP = ip;
            Name = name;
            Reason = reason;
        }

        public Ban()
        {
            IP = string.Empty;
            Name = string.Empty;
            Reason = string.Empty;
        }
    }
}