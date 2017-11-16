
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Xml;

using MySql.Data.MySqlClient;

// - finish separating constructor and initialiser
// - alter flag and value to return 0 when tbl value is null
/*
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
Alter PROCEDURE xSubList
       @ListName nvarchar(255) = null
AS

BEGIN
  SET NOCOUNT ON;

  if @ListName is null
    SELECT * from xList where ListType = 0
  else
    SELECT * from xList where ListType = (Select ListIdx from xList where ListType=0 and EntryDescr = @ListName)
END
GO
-- =============================================
*/

namespace CentralRepositoryAuthPayments
{
    public class xList : IEnumerable<xList.xListRowEntry>
    {
        Defns _defn;
        bool SQLCxnOpened = false;
        MySqlConnection _sqlCxn;
        MySqlCommand sqlCmd;
        MySqlDataReader sqlRdr;

        DataTable xListTable;
        Int32 _ListType = 1; // get overridden when xList is opened but defaults incase of new xList
        String _ListName; // reqd for XML

        String xlTitle = "";
        Int32 xlMaxIdx = 0;
        Boolean _AutoSave;
        //String _dbName;

        public class xListRowEntry
        {
            //  private string descr;
            private int _idx;
            private int _value;
            private DateTime _when;
            private byte _flag;
            public int Idx { get { return _idx; } private set { _idx = value; } }
            public string Descr { get; private set; }
            public string Param { get; private set; }
            public int Value { get { return _value; } private set { _value = value; } }
            public DateTime When { get { return _when; } private set { _when = value; } }
            public byte Flag { get { return _flag; } private set { _flag = value; } }

            public xListRowEntry(DataRow dr)
            {
                int.TryParse(dr[0].ToString(), out _idx);
                Descr = dr[1].ToString();
                Param = dr[2].ToString();
                int.TryParse(dr[3].ToString(), out _value);
                DateTime.TryParse(dr[4].ToString(), out _when);
                byte.TryParse(dr[5].ToString(), out _flag);
            }
        }
        
        public enum Errors
        {
            xList_Idx_cannot_be_set,
            xList_Idx_cannot_be_less_than_zero,
            xList_Idx_Out_of_Range,
            xList_Entry_does_not_exist,
        }

        public enum Fld
        {
            Idx = 0,
            Descr = 1,
            Param = 2,
            Value = 3,
            When = 4,
            Flag = 5
        }

        #region example code to init xList
        /*
            List<Dictionary<xList.Fld, Object>> initList = new List<Dictionary<xList.Fld, object>>();
            Dictionary<xList.Fld, Object> initData;
            initData = new Dictionary<xList.Fld, object>();
            initData.Add(xList.Fld.Idx, 1);
            initData.Add(xList.Fld.Descr, "Christmas Day");
            initData.Add(xList.Fld.When, new DateTime(2016, 12, 25));
            initList.Add(initData);
            initData = new Dictionary<xList.Fld, object>();
            initData.Add(xList.Fld.Idx, 2);
            initData.Add(xList.Fld.Descr, "New Year's Day");
            initData.Add(xList.Fld.When, new DateTime(2016, 1, 1) );
            initList.Add(initData);
        */
        #endregion

        #region constructor
        Boolean isTitleOnTable = false;
        DataRow dr;
        string sqlTxt;
        public xList(string ListName = ""
                   , bool AutoSave = true
                   , string initTitle = ""
                   , List<Dictionary<Fld, Object>> initValues = null
                   , Fld orderBy = Fld.Idx
                   , String dbName = "Common", String dbUser = "", String dbPswd = ""
                   , MySqlConnection sqlCxn = null
                   , String andWhere = ""
                   )
        {
            construct_xList_fst(ListName, AutoSave, orderBy, dbName, dbUser, dbPswd, sqlCxn, andWhere);
            if (ListName == "")
                sqlTxt = "Select * from xList where ListType=0";
            else
            {
                sqlTxt = "Select ListIdx from xList where ListType=0 and EntryDescr=@ED;";
                sqlCmd = new MySqlCommand(sqlTxt, _sqlCxn);
                sqlCmd.Parameters.AddWithValue("@ED", _ListName);
                sqlRdr = sqlCmd.ExecuteReader();
                if (sqlRdr.Read())
                {   // xList already exists
                    if (sqlRdr[0] != DBNull.Value)
                    {
                        int.TryParse(sqlRdr[0].ToString(), out _ListType);
                        sqlRdr.Close();
                    }
                    else
                    {

                    }
                }
                else
                {   // xList does not exist so create at least the index entry [listtype=0]
                    sqlRdr.Close();

                    // must get the next xList idx number from maxidx of listtype=0
                    sqlTxt = "Select max(ListIdx) from xList where ListType=0;";
                    sqlCmd = new MySqlCommand(sqlTxt, _sqlCxn);
                    int.TryParse(sqlCmd.ExecuteScalar().ToString(), out _ListType);
                    ++_ListType;

                    sqlTxt = "Insert xList (ListType,ListIdx,EntryDescr) Values(0,@LI,@ED);";
                    sqlCmd = new MySqlCommand(sqlTxt, _sqlCxn);
                    sqlCmd.Parameters.AddWithValue("@LI", _ListType);
                    sqlCmd.Parameters.AddWithValue("@ED", _ListName);
                    sqlCmd.ExecuteNonQuery();
                    if (initTitle != "")
                    {
                        sqlTxt = "Insert xList (ListType,ListIdx,EntryDescr) Values(@LT,0,@ED);";
                        sqlCmd = new MySqlCommand(sqlTxt, _sqlCxn);
                        sqlCmd.Parameters.AddWithValue("@LT", _ListType);
                        sqlCmd.Parameters.AddWithValue("@ED", initTitle);
                        sqlCmd.ExecuteNonQuery();
                    }
                    if (initValues != null)
                    {
                        foreach (Dictionary<Fld, Object> xListRow in initValues)
                        {
                            dr = xListTable.NewRow();
                            dr["ListIdx"] = MaxIdx + 1;                                     // in case user did not specify the Idx then auto-create it.
                            foreach (KeyValuePair<Fld, Object> xListEntry in xListRow)
                            {
                                string key = (xListEntry.Key == Fld.Idx ? "List" : "Entry") + xListEntry.Key.ToString();
                                dr[key] = xListEntry.Value;
                            }
                            dr["Updated"] = true;
                            dr["Inserted"] = true;
                            xListTable.Rows.Add(dr);
                        }
                        AutoSaveRows(SaveAll: true);
                    }
                }
                sqlTxt = "Select * from xList where ListType=@LT";
            }
            if (andWhere != "") { sqlTxt += $" and ({andWhere})"; }
            sqlTxt += " order by ";
            sqlTxt += (orderBy == Fld.Idx ? "List" : "Entry") + orderBy.ToString();
            sqlTxt += ";";

            sqlCmd = new MySqlCommand(sqlTxt, _sqlCxn);
            sqlCmd.Parameters.AddWithValue("@LT", _ListType);
            sqlRdr = sqlCmd.ExecuteReader();

            while (sqlRdr.Read())
            {
                // if (xListTable.Rows.Count == 0)
                {
                    //_ListType = (int)sqlRdr["ListType"];
                    dr = xListTable.NewRow();
                    // dr["ListIdx"] = 0;
                    if ((int)sqlRdr["ListIdx"] == 0)
                    {
                        dr["ListIdx"] = 0;
                        dr["EntryDescr"] = sqlRdr["EntryDescr"];
                        xlTitle = sqlRdr["EntryDescr"].ToString();
                        dr["EntryParam"] = sqlRdr["EntryParm"];
                        dr["EntryValue"] = sqlRdr["EntryValue"];
                        dr["EntryWhen"] = sqlRdr["EntryWhen"];
                        dr["EntryFlag"] = sqlRdr["EntryFlag"];
                        dr["Updated"] = false;
                        dr["Inserted"] = false;
                        isTitleOnTable = true;
                        xListTable.Rows.InsertAt(dr, 0);
                    }
                }
                if (Convert.ToInt32(sqlRdr["ListIdx"]) > 0)
                {
                    string bert = sqlRdr["ListIdx"].ToString() + "] " + sqlRdr["EntryDescr"].ToString(); // debug
                    Int32 rwIdx = Convert.ToInt32(sqlRdr["ListIdx"]);
                    dr = xListTable.NewRow();
                    dr["ListIdx"] = sqlRdr["ListIdx"];
                    dr["EntryDescr"] = sqlRdr["EntryDescr"];
                    dr["EntryParam"] = sqlRdr["EntryParm"];
                    dr["EntryValue"] = sqlRdr["EntryValue"];
                    dr["EntryWhen"] = sqlRdr["EntryWhen"];
                    dr["EntryFlag"] = sqlRdr["EntryFlag"];
                    dr["Updated"] = false;
                    dr["Inserted"] = false;
                    xListTable.Rows.Add(dr);
                    if (rwIdx > xlMaxIdx)
                        xlMaxIdx = rwIdx;
                }
            }
            sqlRdr.Close();

            if (!isTitleOnTable && _ListName != "")
            {
                dr = xListTable.NewRow();
                dr["ListIdx"] = 0;
                dr["EntryDescr"] = xlTitle = ListName;
                dr["EntryParam"] = System.DBNull.Value;
                dr["EntryValue"] = System.DBNull.Value;
                dr["EntryWhen"] = System.DBNull.Value;
                dr["EntryFlag"] = System.DBNull.Value;
                dr["Inserted"] = true;
                //xListTable.Rows.Add(dr);
                xListTable.Rows.InsertAt(dr, 0); // header always at row zero since I don't think it matters ... maybe
            }

            //if (xListTable.Rows.Count == 0 && autoCreate)
            //    Create(initTitle, initValues);
            //if (xListTable.Rows.Count == 0 && ListName != "")
            //{
            //    if (autoCreate)
            //    {
            //        Create(initTitle, initValues);
            //    }
            //    else
            //    {
            //        _ListName = null;
            //        _ListType = -1;
            //    }
            //}

            if (SQLCxnOpened)
                _sqlCxn.Close();
        }

        // the parms are not unique enough!!!
        //public xList(string ListName = ""
        //           , bool AutoSave = false
        //           , string InitTitle = ""
        //           , string[] InitDescrs = null
        //           , Fld orderBy = Fld.Idx
        //           , String dbName = "Common", String dbUser = "", String dbPswd = ""
        //           , MySqlConnection sqlCxn = null
        //           , String andWhere = ""
        //           )
        //{
        //    construct_xList_fst(ListName, AutoSave, orderBy, dbName, dbUser, dbPswd, sqlCxn, andWhere);
        //}

        private void construct_xList_fst(string ListName=""
                                        , bool AutoSave=false
                   , Fld orderBy = Fld.Idx
                   , String dbName = "Common", String dbUser = "", String dbPswd = ""
                   , MySqlConnection sqlCxn = null
                   , String andWhere = ""
            )
        {
            //Boolean 
            isTitleOnTable = false;

            _ListName = ListName;
            _AutoSave = AutoSave;

            xListTable = new DataTable();
            xListTable.Columns.Add("ListIdx", typeof(Int32));
            xListTable.Columns.Add("EntryDescr", typeof(String));
            xListTable.Columns.Add("EntryParam", typeof(String));
            xListTable.Columns.Add("EntryValue", typeof(Int32));
            xListTable.Columns.Add("EntryWhen", typeof(DateTime));
            xListTable.Columns.Add("EntryFlag", typeof(byte));
            xListTable.Columns.Add("Updated", typeof(Boolean));
            xListTable.Columns.Add("Inserted", typeof(Boolean));
            //DataRow dr;

            if (sqlCxn == null)
            {
                _defn = new Defns(dbName, dbUser, dbPswd);
                _sqlCxn = new MySqlConnection(_defn.cxn());
            }
            else
                _sqlCxn = (MySqlConnection)sqlCxn.Clone();
            if (_sqlCxn.State == ConnectionState.Closed)
            {
                _sqlCxn.Open();
                SQLCxnOpened = true;
            }

        }
        private void construct_xList_lst()
        {

        }
        private void initialise_xList()
        {

        }
        #endregion

        #region item addressable
        public Object this[String Descr, Fld fld = Fld.Param, Boolean useListIdx = false]
        {
            get
            {
                Object rc = null;
                String fldName = (fld == Fld.Idx ? "List" : "Entry") + fld.ToString();
                DataRow[] rw = xListTable.Select("EntryDescr='" + Descr + "'");
                if (rw.Length > 0)
                {
                    if (fld == Fld.Idx && !useListIdx)
                        rc = xListTable.Rows.IndexOf(rw[0]);
                    else
                        rc = rw[0][fldName];
                }
                else
                {
                    throw new System.ArgumentException(Errors.xList_Entry_does_not_exist.ToString(), "xList");
                }
                if (rc == System.DBNull.Value) rc = null;
                return rc;
            }
            set
            {
                if (fld == Fld.Idx)
                {
                    throw new System.ArgumentException(Errors.xList_Idx_cannot_be_set.ToString(), "xList");
                }
                String fldName = "Entry" + fld.ToString();
                if (xListTable.Rows.Count > 0)
                {
                    DataRow[] rw = xListTable.Select("EntryDescr='" + Descr + "'");
                    if (rw.Length > 0)
                    {
                        rw[0][fldName] = value;
                        rw[0]["Updated"] = true;
                        if (_AutoSave) AutoSaveRows();
                    }
                    else
                    {
                        throw new System.ArgumentException(Errors.xList_Entry_does_not_exist.ToString(), "xList");
                    }
                }
            }
        }
        public Object this[Int32 idx, Fld fld = Fld.Descr, Boolean ListIdx = false]
        {
            get
            {
                Object rc = null;
                if (xListTable.Rows.Count > 0)
                {
                    if (0 > idx)
                    {
                        throw new System.ArgumentException(Errors.xList_Idx_cannot_be_less_than_zero.ToString(), "xList");
                    }
                    else if (xListTable.Rows.Count < idx)
                    {
                        throw new System.ArgumentException(Errors.xList_Idx_Out_of_Range.ToString(), "xList");
                    }
                    else
                    {
                        if (!ListIdx)
                        {
                            String fldName = (fld == Fld.Idx ? "List" : "Entry") + fld.ToString();
                            if (ListIdx)
                            {
                                DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
                                if (rw.Length > 0)
                                    rc = rw[0][fldName];
                            }
                            else
                                rc = xListTable.Rows[idx][fldName];
                        }
                        
                    }
                }
                if (rc == System.DBNull.Value) rc = null;
                return rc;
            }
            set
            {
                String fldName = (fld == Fld.Idx ? "List" : "Entry") + fld.ToString();
                if (ListIdx)
                {
                    DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
                    if (rw.Length > 0)
                    {
                        rw[0][fldName] = value;
                        rw[0]["Updated"] = true;
                        if (_AutoSave) AutoSaveRows();
                    }
                }
                else
                {
                    xListTable.Rows[idx][fldName] = value;
                    xListTable.Rows[idx][fldName] = value;
                }
            }
        }
        public xListRowEntry this[int Idx]
        {
            get
            {
                return new xListRowEntry(xListTable.Rows[Idx]);
            }
        }
        #endregion

        // 'built-in' methods
        public Boolean isAutoSaveOn { get { return _AutoSave; } }
        public Boolean Contains(String Descr, Fld fld = Fld.Descr) { return IndexOf(Descr, fld, false) > 0; }
        public Int32 Count { get { return xListTable.Rows.Count; } } // because title is held in row 0!
        //public Int32 EntryCount { get { return ((DataRow[])xListTable.Select("ListIdx>0")).r.Rows.Count; } } // because title is held in row 0!
        public Int32 EntryCount { get { return (xListTable.Select("ListIdx>0").Count<DataRow>()); } } // because title is held in row 0!
        public Int32 FullCount { get { return xListTable.Rows.Count; } } // because title is held in row 0!
        public Int32 IndexOf(Object SearchFor, Fld fld = Fld.Descr, Boolean useListIdx = false)
        {
            Int32 rc = -1;
            String fldName = (fld == Fld.Idx ? "List" : "Entry") + fld.ToString();
            DataRow[] rw = xListTable.Select(fldName + "='" + SearchFor + "'");
            if (rw.Length > 0)
            {
                if (useListIdx && fld == Fld.Idx)
                    rc = Convert.ToInt32(rw[0]["ListIdx"]);
                else
                    rc = xListTable.Rows.IndexOf(rw[0]);
            }
            return rc;
        }
        //public Int32 IndexOfFlag(Byte Flag, Int32 StartAt = 0, Boolean ListIdx = false)
        //{
        //    Int32 rc = 0;
        //    DataRow[] rw = xListTable.Select("EntryFlag='" + Flag + "'" + (StartAt == 0 ? "" : " and ListIdx>=" + StartAt.ToString()));
        //    if (rw.Length > 0) rc = Convert.ToInt32(rw[0]["ListIdx"]);
        //    return rc;
        //}

        // xList special methods
        public Int32 MaxIdx
        {
            get
            {
                Int32 rc = 0;
                DataRow[] rw = xListTable.Select("ListIdx = Max(ListIdx)");
                if (rw.Length > 0)
                    rc = Convert.ToInt32(rw[0]["ListIdx"]);
                return rc;
            }
        }

        public string Title
        {
            get
            {
                String rc = "";
                DataRow[] rw = xListTable.Select("ListIdx=0");
                if (rw.Length > 0)
                    rc = rw[0]["EntryDescr"].ToString();
                else
                    rc = xlTitle;
                return Azotus.Defns.LeftBrkt + rc + Azotus.Defns.RghtBrkt;
            }
            set
            {
                xlTitle = value;
                if (xlTitle.Substring(0, 1) == Azotus.Defns.LeftBrkt) { xlTitle = xlTitle.Substring(1); }
                if (xlTitle.Substring(xlTitle.Length-1, 1) == Azotus.Defns.RghtBrkt) { xlTitle = xlTitle.Substring(0, xlTitle.Length - 1); }
                DataRow[] rw = xListTable.Select("ListIdx=0");
                if (rw.Length > 0)
                {
                    Int32 idx = Convert.ToInt32(rw[0]["ListIdx"]);
                    rw[0]["EntryDescr"] = value;
                    rw[0]["Updated"] = true; // title will auto-create as required.
                    if (_AutoSave) AutoSaveRows();
                }
            }
        }
        //public String xDescr(int idx, String Descr = null)
        //{
        //    String rc = "";

        //    if (Descr != null)
        //    {

        //    }

        //    DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
        //    if (rw.Length > 0) rc = rw[0]["EntryDescr"].ToString();
        //    return rc;
        //}
        public Int32 Idx(int idx)
        {
            Int32 rc = -1;
            DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
            if (rw.Length > 0) rc = Convert.ToInt32(rw[0]["ListIdx"]);
            return rc;
        }
        public Int32 Idx(String Descr)
        {
            Int32 rc = -1;
            try
            {
                DataRow[] rw = xListTable.Select("EntryDescr='" + Descr + "'");
                if (rw.Length > 0) rc = Convert.ToInt32(rw[0]["ListIdx"]);
            }
            catch (Exception exc)
            {
                string bert = exc.Message;
            }
            return rc;
        }
        public String Param(int idx)
        {
            String rc = "";
            DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
            if (rw.Length > 0) rc = rw[0]["EntryParam"].ToString();
            return rc;
        }
        public String Param(String Descr)
        {
            //String rc = "";
            //DataRow[] rw = xListTable.Select("EntryDescr='" + Descr + "'");
            //if (rw.Length > 0) rc = rw[0]["EntryParam"].ToString();
            return Param(IndexOf(Descr));
        }
        public Int32 Value(int idx)
        {
            Int32 rc = 0;
            DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
            if (rw.Length > 0 && rw[0]["EntryValue"] != System.DBNull.Value) rc = Convert.ToInt32(rw[0]["EntryValue"]);
            return rc;
        }
        public Int32 Value(String Descr)
        {
            return Value(IndexOf(Descr));
        }
        public DateTime When(int idx)
        {
            DateTime rc = DateTime.MinValue;
            DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
            if (rw.Length > 0 && rw[0]["EntryWhen"] != DBNull.Value) rc = Convert.ToDateTime(rw[0]["EntryWhen"]);
            return rc;
        }
        public DateTime When(String Descr)
        {
            return When(IndexOf(Descr));
        }
        public Byte Flag(Int32 idx)
        {
            Byte rc = 0;
            DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
            if (rw.Length > 0) if (rw[0]["EntryFlag"] != DBNull.Value) rc = Convert.ToByte(rw[0]["EntryFlag"]);
            return rc;
        }
        public Byte Flag(String Descr)
        {
            return Flag(IndexOf(Descr));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="flagPosn">0-7 bit position within flag</param>
        /// <returns></returns>
        //public Boolean Flag(Int32 idx, int flagPosn)
        //{
        //    Boolean rc = false;
        //    DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
        //    if (rw.Length > 0) if (rw[0]["EntryFlag"] != DBNull.Value) rc = ((Convert.ToByte(rw[0]["EntryFlag"])) & (1 << flagPosn)) > 0;
        //    return rc;
        //}
        public Boolean Flag(Int32 idx, int flagPosn, Boolean ListIdx = false)
        {
            Boolean rc = false;
            if (ListIdx)
            {
                DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
                if (rw.Length > 0) if (rw[0]["EntryFlag"] != DBNull.Value) rc = ((Convert.ToByte(rw[0]["EntryFlag"])) & (1 << flagPosn)) > 0;
            }
            else
                rc = ((Convert.ToByte(xListTable.Rows[idx]["EntryFlag"])) & (1 << flagPosn)) > 0;
            return rc;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="flagPosn">0-7 bit position within flag</param>
        /// <returns></returns>
        public Boolean Flag(String Descr, int flagPosn)
        {
            return Flag(IndexOf(Descr), flagPosn);
        }

        public void SetDescr(Int32 idx, String value)
        {
            DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
            if (rw.Length > 0)
            {
                rw[0]["EntryDescr"] = value;
                rw[0]["Updated"] = true;
                if (_AutoSave) AutoSaveRows();
            }
        }
        public void SetDescr(String Descr, String value)
        {
            SetDescr(IndexOf(Descr), value);
        }
        public void SetParam(Int32 idx, String value)
        {
            DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
            if (rw.Length > 0)
            {
                rw[0]["EntryParam"] = value;
                rw[0]["Updated"] = true;
                if (_AutoSave) AutoSaveRows();
            }
        }
        public void SetParam(String Descr, String value)
        {
            SetParam(IndexOf(Descr), value);
        }
        public void SetValue(Int32 idx, Int32 value)
        {
            DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
            if (rw.Length > 0)
            {
                rw[0]["EntryValue"] = value;
                rw[0]["Updated"] = true;
                if (_AutoSave) AutoSaveRows();
            }
        }
        public void SetValue(String Descr, Int32 value)
        {
            SetValue(IndexOf(Descr), value);
        }
        public void SetWhen(Int32 idx, DateTime value)
        {
            DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
            if (rw.Length > 0)
            {
                rw[0]["EntryWhen"] = value;
                rw[0]["Updated"] = true;
                if (_AutoSave) AutoSaveRows();
            }
        }
        public void SetWhen(String Descr, DateTime value)
        {
            SetWhen(IndexOf(Descr), value);
        }

        public void SetFlag(Int32 idx, Byte value)
        {
            DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
            if (rw.Length > 0)
            {
                rw[0]["EntryFlag"] = value;
                rw[0]["Updated"] = true;
                if (_AutoSave) AutoSaveRows();
            }
        }
        public void SetFlag(String Descr, Byte value)
        {
            SetFlag(IndexOf(Descr), value);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="value"></param>
        /// <param name="flagPosn">0-7 bit position within flag</param>
        //public void SetFlag(Int32 idx, Boolean value, Int32 flagPosn)
        //{
        //    DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
        //    if (rw.Length > 0)
        //    {

        //        rw[0]["EntryFlag"] = (value ? (Byte)rw[0]["EntryFlag"] | 1 << flagPosn : (Byte)rw[0]["EntryFlag"] & 255 - (1 << flagPosn));
        //        rw[0]["Updated"] = true;
        //        if (_AutoSave) autoSave();
        //    }
        //}
        //public void SetFlag(String Descr, Boolean value, Int32 flagPosn)
        //{
        //    SetFlag(IndexOf(Descr), value, flagPosn);
        //}
        public void SetFlag(Int32 idx, Boolean value, Int32 flagPosn, Boolean ListIdx = false)
        {
            if (ListIdx)
            {
                DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
                if (rw.Length > 0)
                {
                    rw[0]["EntryFlag"] = (value ? (Byte)rw[0]["EntryFlag"] | 1 << flagPosn : (Byte)rw[0]["EntryFlag"] & 255 - (1 << flagPosn));
                    rw[0]["Updated"] = true;
                    if (_AutoSave) AutoSaveRows();
                }
            }
            else
            {
                object o1 = xListTable.Rows[idx]["EntryFlag"];
                byte oldFlagValue = (byte)(o1 == System.DBNull.Value ? 0 : o1);
                xListTable.Rows[idx]["EntryFlag"] = (value ? (Byte)(oldFlagValue) | 1 << flagPosn : (Byte)(oldFlagValue) & 255 - (1 << flagPosn));
                xListTable.Rows[idx]["Updated"] = true;
                if (_AutoSave) AutoSaveRows();
            }
        }
        public void SetFlag(String Descr, Boolean value, Int32 flagPosn)
        {
            SetFlag(IndexOf(Descr), value, flagPosn);
        }

        public bool Add(Dictionary<Fld, object> xListRow)
        {
            bool rc = false;
            string sqlTxt1 = "Insert xList (ListType";
            string sqlTxt2 = ") Values(@ListType";
            sqlCmd = new MySqlCommand("", _sqlCxn);

            foreach (KeyValuePair<Fld, Object> xListEntry in xListRow)
            {
                string key = (xListEntry.Key == Fld.Idx ? "List" : "Entry") + xListEntry.Key.ToString();
                sqlTxt1 += ", " + key;
                sqlTxt2 += ", @" + key;
                sqlCmd.Parameters.AddWithValue("@" + key, xListEntry.Value);
            }
            sqlCmd.CommandText = sqlTxt1 + sqlTxt2 + ");";
            rc = (sqlCmd.ExecuteNonQuery() > 0);

            if (rc) Append(xListRow);










            return rc;
        }
        public Boolean Add(Dictionary<String, String> xListRow)
        { // don't forget to update xlMaxIdx
            Int32 rc = 0;
            if (!Contains(xListRow["EntryDescr"].ToString()))
            {
                String sqlTxt;
                MySqlCommand sqlCmd;

                sqlTxt = "Select Max(ListIdx) as mIdx where ListType=@T;";
                sqlCmd = new MySqlCommand(sqlTxt, _sqlCxn);
                xlMaxIdx = Convert.ToInt32(sqlCmd.ExecuteScalar()) + 1;

                sqlTxt = "Insert xList (ListType,ListIdx,EntryDescr,EntryParm,EntryValue,EntryWhen,EntryFlag) Values(@T,@I,@D,@P,@V,@W,@F);";
                sqlCmd = new MySqlCommand(sqlTxt, _sqlCxn);
                sqlCmd.Parameters.AddWithValue("@T", _ListType);
                sqlCmd.Parameters.AddWithValue("@I", xlMaxIdx);
                sqlCmd.Parameters.AddWithValue("@D", xListRow["EntryDescr"]);
                sqlCmd.Parameters.AddWithValue("@P", xListRow["EntryParam"]);
                sqlCmd.Parameters.AddWithValue("@V", xListRow["EntryValue"]);
                sqlCmd.Parameters.AddWithValue("@W", xListRow["EntryWhen"]);
                sqlCmd.Parameters.AddWithValue("@F", xListRow["EntryFlag"]);
                _sqlCxn.Open();
                rc += sqlCmd.ExecuteNonQuery();
                _sqlCxn.Close();

                if (rc > 0)
                {
                    Append(xListRow);
                }
            }
            return (rc > 0);
        }
        public bool Add(string descr, string param = null, int? value = null, DateTime? when = null, byte? flag = null)
        {
            bool rc = false;

            if (!Contains(descr))
            {
                MySqlCommand SQLCmd = new MySqlCommand();
                string sqlTxt1 = "Insert xList (ListType,ListIdx,EntryDescr";
                string sqlTxt2 = ") Values(@LT,@LI,@ED";
                SQLCmd.Parameters.AddWithValue("@LT", _ListType);
                SQLCmd.Parameters.AddWithValue("@LI", MaxIdx + 1);
                SQLCmd.Parameters.AddWithValue("@ED", descr);
                if (param != null) { sqlTxt1 += ",EntryParm"; sqlTxt2 += ",@EP"; SQLCmd.Parameters.AddWithValue("@EP", param); }
                if (value.HasValue) { sqlTxt1 += ",EntryValue"; sqlTxt2 += ",@EV"; SQLCmd.Parameters.AddWithValue("@EV", value); }
                if (when.HasValue) { sqlTxt1 += ",EntryWhen"; sqlTxt2 += ",@EW"; SQLCmd.Parameters.AddWithValue("@EW", when); }
                if (flag.HasValue) { sqlTxt1 += ",EntryFlag"; sqlTxt2 += ",@EF"; SQLCmd.Parameters.AddWithValue("@EF", flag); }
                String sqlTxt = sqlTxt1 + sqlTxt2 + ");";

                SQLCmd.Connection = _sqlCxn;
                SQLCmd.CommandText = sqlTxt;
                _sqlCxn.Open();
                rc = (SQLCmd.ExecuteNonQuery() == 1);
                _sqlCxn.Close();
                if (rc)
                {
                    // xlCount++;
                    Append(MaxIdx + 1, descr, param, value, when, flag);
                }
            }
            return rc;
        }

        public void Append(Dictionary<Fld, object> xListRow)
        {
            DataRow xRow = xListTable.NewRow();
            foreach (KeyValuePair<Fld, Object> xListEntry in xListRow)
            {
                string key = (xListEntry.Key == Fld.Idx ? "List" : "Entry") + xListEntry.Key.ToString();
                xRow[key] = xListEntry.Value;
            }
            xRow["Updated"] = false;
            xRow["Inserted"] = true; // hmm, maybe, maybe not
            xListTable.Rows.Add(xListRow);
        }
        public void Append(Dictionary<String, String> xListRow)
        {   // when appending, set listidx = -1 unless already specified
            // don't forget to update xlMaxIdx
            DataRow xRow = xListTable.NewRow();
            foreach (KeyValuePair<string,string> kvp in xListRow)
            {
                xRow[kvp.Key] = kvp.Value;
            }
            xRow["Updated"] = false;
            xRow["Inserted"] = false;
            xListTable.Rows.Add(xListRow);
        }
        public void Append(string descr, string param = null, int? value = null, DateTime? when = null, byte? flag = null)
        {
            Append(MaxIdx + 1, descr, param, value, when, flag);
        }
        private void Append(int listIdx, string descr, string param = null, int? value = null, DateTime? when = null, byte? flag = null)
        {
            DataRow xRow;
            xRow = xListTable.NewRow();
            xRow["ListIdx"] = listIdx;
            xRow["EntryDescr"] = descr;
            xRow["EntryParam"] = param;
            if (value == null) xRow["EntryValue"] = System.DBNull.Value; else xRow["EntryValue"] = value;
            if (when == null) xRow["EntryWhen"] = System.DBNull.Value; else xRow["EntryWhen"] = when;
            if (flag == null) xRow["EntryFlag"] = System.DBNull.Value; else xRow["EntryFlag"] = flag;
            xListTable.Rows.Add(xRow);
        }

        private Int32 AutoSaveRows(Int32 rwIdx = -1, Boolean SaveAll = false)
        { // can I pass in a row idx to save a row rather than having to do the foreach loop as below
            Boolean sqlCxnAlreadyOpen = true;
            Int32 rc = 0;
            String sqlTxt = "";
            MySqlCommand sqlCmd = new MySqlCommand(sqlTxt, _sqlCxn);
            //[+] only open if #updateds > 0!
            if (_sqlCxn.State != ConnectionState.Open)
            {
                _sqlCxn.Open();
                sqlCxnAlreadyOpen = false;
            }

            if (_ListType == -1)
            {   // brand new xList
                sqlTxt = "Select Max(ListIdx) from xList where ListType=0;";
                sqlCmd.CommandText = sqlTxt;
                _ListType = Convert.ToInt32(sqlCmd.ExecuteScalar()) + 1;
                sqlTxt = "Insert xList (ListType,ListIdx,EntryDescr) Values(@LT,@LI,@ED);";
                sqlCmd.Parameters.Clear();
                sqlCmd.Parameters.AddWithValue("@LT", 0);
                sqlCmd.Parameters.AddWithValue("@LI", _ListType);
                sqlCmd.Parameters.AddWithValue("@ED", _ListName);
                sqlCmd.CommandText = sqlTxt;
                sqlCmd.ExecuteNonQuery();
            }

            foreach (DataRow rw in xListTable.Rows)
            {
                sqlTxt = "";
                sqlCmd.Parameters.Clear();

                if ((rw["Updated"] != System.DBNull.Value && Convert.ToBoolean(rw["Updated"])) || SaveAll)
                {
                    if (rw["Inserted"] != System.DBNull.Value && Convert.ToBoolean(rw["Inserted"]))
                        sqlTxt = "Insert xList (ListType,ListIdx,EntryDescr,EntryParm,EntryValue,EntryWhen,EntryFlag) Values(@LT,@LI,@ED,@EP,@EV,@EW,@EF);";
                    else
                        sqlTxt = "Update xList Set EntryDescr=@ED,EntryParm=@EP,EntryValue=@EV,EntryWhen=@EW,EntryFlag=@EF where ListType=@LT and ListIdx=@LI;";
                }

                if (sqlTxt != "")
                {
                    sqlCmd.Parameters.AddWithValue("@LT", _ListType);
                    sqlCmd.Parameters.AddWithValue("@LI", rw["ListIdx"]);
                    sqlCmd.Parameters.AddWithValue("@ED", rw["EntryDescr"]);
                    sqlCmd.Parameters.AddWithValue("@EP", rw["EntryParam"]);
                    sqlCmd.Parameters.AddWithValue("@EV", rw["EntryValue"]);
                    sqlCmd.Parameters.AddWithValue("@EW", rw["EntryWhen"]);
                    sqlCmd.Parameters.AddWithValue("@EF", rw["EntryFlag"]);
                    sqlCmd.CommandText = sqlTxt;

                    rc += sqlCmd.ExecuteNonQuery();
                }
            }
            if (!sqlCxnAlreadyOpen) _sqlCxn.Close();
            return rc;
        }

        public void Create(String initTitle = "", List<Dictionary<Fld, Object>> initValues = null)
        {
            DataRow dr;

            _ListType = -1;
            if (initTitle != "")
            {
                dr = xListTable.NewRow();
                dr["ListIdx"] = 0;
                dr["EntryDescr"] = initTitle;
                dr["Updated"] = true;
                dr["Inserted"] = true;
                xListTable.Rows.Add(dr);
            }

            if (null != initValues)
            {
                foreach (Dictionary<Fld, Object> xListRow in initValues)
                {
                    dr = xListTable.NewRow();
                    foreach (KeyValuePair<Fld, Object> xListEntry in xListRow)
                        dr[(xListEntry.Key == Fld.Idx ? "List" : "Entry") + xListEntry.Key.ToString()] = xListEntry.Value;
                    dr["Updated"] = true;
                    dr["Inserted"] = true;
                    xListTable.Rows.Add(dr);
                }
                
            } if (_AutoSave) AutoSaveRows(SaveAll: true);
        }

        public void Create(String[] Descr, String[] Param = null, Int32[] Value = null, DateTime[] When = null, Byte[] Flag = null)
        {
            DataRow dr;
            xListTable.Clear();

            dr = xListTable.NewRow();
            dr["ListIdx"] = 0;
            dr["EntryDescr"] = xlTitle;
            dr["EntryParam"] = System.DBNull.Value;
            dr["EntryValue"] = System.DBNull.Value;
            dr["EntryWhen"] = System.DBNull.Value;
            dr["EntryFlag"] = System.DBNull.Value;
            xListTable.Rows.Add(dr);

            for (int Lp1 = 0; Lp1 < Descr.Length; Lp1++)
            {
                dr = xListTable.NewRow();
                dr["ListIdx"] = Lp1 + 1;
                dr["EntryDescr"] = Descr[Lp1];
                if (Param != null) if (Lp1 < Param.Length) dr["EntryParam"] = Param[Lp1]; else dr["EntryParam"] = System.DBNull.Value;
                if (Value != null) if (Lp1 < Value.Length) dr["EntryValue"] = Value[Lp1]; else dr["EntryValue"] = System.DBNull.Value;
                if (When != null) if (Lp1 < When.Length) dr["EntryWhen"] = When[Lp1]; else dr["EntryWhen"] = System.DBNull.Value;
                if (Flag != null) if (Lp1 < Flag.Length) dr["EntryFlag"] = Flag[Lp1]; else dr["EntryFlag"] = System.DBNull.Value;
                xListTable.Rows.Add(dr);
            }

            if (_AutoSave) Save();
        }
        public void Save()
        {
            AutoSaveRows(SaveAll: true);
        }

        public DataTable rawTable { get { return xListTable; } }
        public String Version
        {
            get { return _version; }
        }

        public void WriteToXML(String fileName, String filePath = "")
        {
            List<Dictionary<string, string>> stuff = new List<Dictionary<string, string>>();

            XmlDocument xdoc;

            xdoc = new XmlDocument();
            xdoc.LoadXml("<product><name>" + "?xlist_Title?" + "</name></product>");

            foreach (Dictionary<string, string> bits in stuff)
            {
                XmlElement newElem = xdoc.CreateElement("change");

                foreach (KeyValuePair<string, string> kvp in bits)
                {
                    XmlElement subElem = xdoc.CreateElement(kvp.Key);
                    subElem.InnerText = kvp.Value;
                    newElem.AppendChild(subElem);
                }

                xdoc.DocumentElement.AppendChild(newElem);
            }

            XmlWriterSettings stgs = new XmlWriterSettings();
            stgs.Indent = true;
            XmlWriter wrt = XmlWriter.Create(@"D:\xml1.xml", stgs);
            xdoc.Save(wrt);

            xdoc = new XmlDocument();
            XmlElement newElmnt;
            XmlElement subElmnt;

            xdoc.LoadXml("<xlist><name>" + _ListName + "</name></xlist>");

            foreach (DataRow rw in xListTable.Rows)
            {
                newElmnt = xdoc.CreateElement("row");
                subElmnt = xdoc.CreateElement("ListIdx");
                subElmnt.InnerText = rw["ListIdx"].ToString();
                newElmnt.AppendChild(subElmnt);
                subElmnt = xdoc.CreateElement("EntryDescr");
                subElmnt.InnerText = rw["EntryDescr"].ToString();
                newElmnt.AppendChild(subElmnt);
                subElmnt = xdoc.CreateElement("EntryParam");
                subElmnt.InnerText = rw["EntryParam"].ToString();
                newElmnt.AppendChild(subElmnt);
                subElmnt = xdoc.CreateElement("EntryValue");
                subElmnt.InnerText = rw["EntryValue"].ToString();
                newElmnt.AppendChild(subElmnt);
                subElmnt = xdoc.CreateElement("EntryWhen");
                subElmnt.InnerText = rw["EntryWhen"].ToString();
                newElmnt.AppendChild(subElmnt);
                subElmnt = xdoc.CreateElement("EntryFlag");
                subElmnt.InnerText = rw["EntryFlag"].ToString();
                newElmnt.AppendChild(subElmnt);

                xdoc.DocumentElement.AppendChild(newElmnt);
            }
            XmlWriter xListWrt = XmlWriter.Create(@"D:\xml2.xml", stgs);
            xdoc.Save(xListWrt);
            xListWrt.Close();
        }

        public void ReadFromXML(String fileName, String filePath = "", Boolean xListReplace = false)
        {
            // replace will over-write existing xList
            // not replace will merely add any new rows to end of existing xList, once read in from d/b

            String _fileName = Path.Combine(filePath, fileName);
            Dictionary<string, string> xListRow = new Dictionary<string, string>();

            // commented because overwriting an xList is considered bad.
            //if (xListReplace)
            //{
            //    // add lines to xList when > max ctr
            //    // otherwise clear xList here
            //}

            using (XmlReader xmlRdr = XmlReader.Create(_fileName))
            {
                string attrNode, attrName = "", attrData = "";

                while (xmlRdr.Read())
                {
                    if (xmlRdr.HasAttributes) // ignore for now
                        for (int Lp1 = 0; Lp1 < xmlRdr.AttributeCount; Lp1++)
                        {
                            attrNode = xmlRdr.Name;
                            attrNode = xmlRdr[Lp1].ToString();
                        }

                    string chk = XmlNodeType.Element.ToString();
                    string chk2;
                    switch (xmlRdr.NodeType)
                    {
                        case XmlNodeType.Element:
                            {
                                ; attrName = xmlRdr.Name.TrimEnd('\n', '\r');
                                break;
                            }

                        case XmlNodeType.Text:
                            {
                                attrData = xmlRdr.Value.ToString().TrimEnd('\n', '\r');
                                break;
                            }

                        case XmlNodeType.EndElement:
                            {
                                chk2 = xmlRdr.Name;
                                switch (xmlRdr.Name.ToLower())
                                {
                                    case "name":
                                        {
                                            // create the xList, perhaps.
                                            break;
                                        }
                                    case "row":
                                        {
                                            // add to xlist and save
                                            if (xListReplace || Convert.ToInt32(xListRow["ListIdx"]) > xlMaxIdx)
                                            {
                                                Add(xListRow);
                                            }
                                            xListRow.Clear();
                                            break;
                                        }
                                    default:
                                        {
                                            xListRow.Add(xmlRdr.Name.ToLower(), attrData);
                                            attrData = "";
                                            break;
                                        }
                                }
                                break;
                            }
                        default: { break; }
                    }
                    //if (attrName == "product" && attrData != _product)
                    //{
                    //    xmlRdr.MoveToElement();
                    //}
                }
                xmlRdr.Close();
            }
        }

        #region IEnumerator
        public xListRowEntry xListEntry(int idx)
        {
            return new xListRowEntry(xListTable.Rows[idx]);
        }
 
        public IEnumerator<xListRowEntry> GetEnumerator()
        {
            foreach (DataRow dr in xListTable.Rows)
            {
                xListRowEntry xre = new xListRowEntry(dr);
                yield return xre;
            }
        }
 
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
            // throw new NotImplementedException();
        }
        #endregion
        
        /* xList:
         * v.1.1.001 - 04-Apr-2016 - AjD - Added xList title or ListName to data table
         *                               - Added leftbrkt and rghtbrkt to xlist.title
         * v.1.1.002 - 13-Apr-2016 - AjD - Included xTitle in datatable when list title not in SQL table
         * v.1.2.001 - 04-Nov-2016 - AjD - Finished merging code with version developed whilst at UPS
         * v.1.2.002 - 14-Dec-2016 - AjD - removed autocreate option since wouldn't be using an xList unless it was wanted.
         */
        public const string _version = "v.01.002.0001";
    }
}
