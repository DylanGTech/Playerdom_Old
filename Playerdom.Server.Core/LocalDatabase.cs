using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Playerdom.Server.Core.Data;
using SQLite;

namespace Playerdom.Server.Core
{
    public class LocalDatabase : IDisposable
    {
        private SQLiteConnection db;


        public LocalDatabase(string path)
        {
            string directoryPath = Path.Combine(path, "Data");

            if(!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);

            }

            if (File.Exists(Path.Combine(directoryPath, "Playerdom.db")))
            {
                db = new SQLiteConnection(Path.Combine(directoryPath, "Playerdom.db"), SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
            }
            else
            {
                db = new SQLiteConnection(Path.Combine(directoryPath, "Playerdom.db"), SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
                db.CreateTable<PlayerEntry>();
            }
        }

        public long? GetPlayerID(Guid token)
        {
            List<PlayerEntry> matches = db.Query<PlayerEntry>("select * from PlayerEntry where Token = ?", token);

            if (matches == null || matches.Count == 0) return null;

            return matches[0].Id;
        }

        public Guid? GetPlayerToken(long id)
        {
            List<PlayerEntry> matches = db.Query<PlayerEntry>("select * from PlayerEntry where Id = ?", id);

            if (matches == null || matches.Count == 0) return null;

            return matches[0].Token;
        }

        public void UpdatePlayerToken(long id, Guid newToken)
        {
            db.Execute("update PlayerEntry set Token = ? where id = ?", newToken, id);
        }

        public long CreateNewPlayer(Guid token)
        {
            db.Insert(new PlayerEntry() { Token = token }) ;

            return GetPlayerID(token).Value;
        }


        public void Dispose()
        {
            db.Dispose();
        }
    }
}
