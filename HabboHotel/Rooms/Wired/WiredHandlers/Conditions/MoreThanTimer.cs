﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pici.HabboHotel.Rooms.Wired.WiredHandlers.Interfaces;
using Pici.Storage.Database.Session_Details.Interfaces;
using System.Data;
using Pici.HabboHotel.Items;

namespace Pici.HabboHotel.Rooms.Wired.WiredHandlers.Conditions
{
    class MoreThanTimer : IWiredCondition
    {
        private int timeout;
        private Room room;
        private RoomItem item;
        private bool isDisposed = false;

        public MoreThanTimer(int timeout, Room room, RoomItem item)
        {
            this.timeout = timeout;
            this.room = room;
            this.isDisposed = false;
            this.item = item;
        }

        public bool AllowsExecution(RoomUser user)
        {
            if (room.lastTimerReset == null)
                return false;

            TimeSpan sinceTimerReset = DateTime.Now - room.lastTimerReset;
            return (sinceTimerReset.TotalSeconds > timeout);
        }

        public void SaveToDatabase(IQueryAdapter dbClient)
        {
            if (dbClient.dbType == Pici.Storage.Database.DatabaseType.MSSQL)
            {
                dbClient.runFastQuery("DELETE FROM trigger_item WHERE trigger_id = " + item.Id);
                dbClient.setQuery("REPLACE INTO trigger_item SET trigger_id = @id, trigger_input = 'integer',  trigger_data = @trigger_data , all_user_triggerable = 0");
            }
            else
                dbClient.setQuery("REPLACE INTO trigger_item SET trigger_id = @id, trigger_input = 'integer',  trigger_data = @trigger_data , all_user_triggerable = 0");

            dbClient.addParameter("id", (int)this.item.Id);
            dbClient.addParameter("trigger_data", timeout);
            dbClient.runQuery();
        }

        public void LoadFromDatabase(IQueryAdapter dbClient, Room insideRoom)
        {
            dbClient.setQuery("SELECT trigger_data FROM trigger_item WHERE trigger_id = @id ");
            dbClient.addParameter("id", (int)this.item.Id);
            DataRow dRow = dbClient.getRow();
            if (dRow != null)
            {
                this.timeout = Convert.ToInt32(dRow[0].ToString());
            }
            else
            {
                timeout = 20;
            }
        }

        public void DeleteFromDatabase(IQueryAdapter dbClient)
        {
            dbClient.runFastQuery("DELETE FROM trigger_item WHERE trigger_id = '" + this.item.Id + "'");
        }

        public void Dispose()
        {
            isDisposed = true;
            item = null;
            room = null;
        }

        public bool Disposed()
        {
            return isDisposed;
        }
    }
}
