    /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
     * 
     * Create Command [MS SQL Server] - use Alt-Shift-Cursor-Arrow-Keys to select for copy and paste
     * 
     * CREATE TABLE [dbo].[xList](
     * 	[Idx] [int] IDENTITY(1,1) NOT NULL,
     * 	[ListType] [int] NOT NULL,
     * 	[ListIdx] [int] NOT NULL,
     * 	[EntryDescr] [nvarchar](256) NOT NULL,
     * 	[EntryParm] [nvarchar](256) NULL,
     * 	[EntryValue] [int] NULL,
     * 	[EntryWhen] [date] NULL,
     * 	[EntryFlag] [tinyint] NULL,
     *  CONSTRAINT [xList_Idx] PRIMARY KEY CLUSTERED 
     * (
     * 	[Idx] ASC
     * )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
     *  CONSTRAINT [xListTypeDescr] UNIQUE NONCLUSTERED 
     * (
     * 	[ListType] ASC,
     * 	[EntryDescr] ASC
     * )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
     *  CONSTRAINT [xListTypeIdx] UNIQUE NONCLUSTERED 
     * (
     * 	[ListType] ASC,
     * 	[ListIdx] ASC
     * )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
     * ) ON [PRIMARY]
     * 
     * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
     * 
     * CREATE PROCEDURE [dbo].[xSubList]
     * -- Add the parameters for the stored procedure here
     * @ListName nvarchar(255) = null
     * AS
     * BEGIN
     * 
     * -- SET NOCOUNT ON added to prevent extra result sets from
     * -- interfering with SELECT statements.
     * SET NOCOUNT ON;
     * 
     * -- Insert statements for procedure here
     * if @ListName is null
     *     SELECT * from xList where ListType = 0
     * else
     *     SELECT * from xList where ListType = (Select ListIdx from xList where ListType=0 and EntryDescr = @ListName)
     * END
     * 
     * 
     * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
    public class xList : IEnumerable<xList.xListRowEntry>
    {
        adDBDefns defn;
        SqlConnection sqlCxn;
        DataTable xListTable;
        String _ListName;
        Int32 _ListType;

        String xlTitle = "";
        Boolean _AutoSave;
        //String _dbName;

        #region delegates
        // experimentation with Delegates
        delegate string ConcatDelegate(string input);

        static string DisplayTitle1(string title)
        {
            return adFns.LeftBrkt + title + adFns.RghtBrkt;
        }
        #endregion

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
            xList_Entry_Does_Not_Exist
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

        // core functionality
        #region constructor
        public xList(string ListName
                  , Boolean autoSave = false
                  , Boolean autoCreate = false
                  , String initTitle = ""
                  , List<Dictionary<Fld, Object>> initValues = null
                  , Fld orderBy = Fld.Idx
                  , String andWhere = ""
                  , String dbName = "")
        {
            _ListName = ListName;
            _AutoSave = autoSave;
            xlTitle = "";
            defn = new adDBDefns(dbName);

            xListTable = new DataTable();
            xListTable.Columns.Add("ListIdx", typeof(Int32));
            xListTable.Columns.Add("EntryDescr", typeof(String));
            xListTable.Columns.Add("EntryParam", typeof(String));
            xListTable.Columns.Add("EntryValue", typeof(Int32));
            xListTable.Columns.Add("EntryWhen", typeof(DateTime));
            xListTable.Columns.Add("EntryFlag", typeof(byte));
            xListTable.Columns.Add("Updated", typeof(Boolean));
            xListTable.Columns.Add("Inserted", typeof(Boolean));
            DataRow dr;

            sqlCxn = new SqlConnection(defn.cxn());
            SqlCommand sqlCmd;
            sqlCxn.Open();
            string sqlTxt;
            sqlTxt = "Select Min(ListIdx) from xList where ListType=0";
            if (ListName != "")
                sqlTxt += " and EntryDescr=@ED";
            sqlTxt += ";";
            sqlCmd = new SqlCommand(sqlTxt, sqlCxn);
            sqlCmd.Parameters.AddWithValue("@ED", ListName);
            int.TryParse(sqlCmd.ExecuteScalar().ToString(), out _ListType);
            if (!(_ListType == 0 && _ListName != "")) // || (_ListType == 0 && _ListName == ""))
            {
                sqlTxt = "Select * from xList where ListType=@LT";
                if (andWhere != "") { sqlTxt += $" and ({andWhere})"; }
                sqlTxt += " order by ";
                sqlTxt += (orderBy == Fld.Idx ? "List" : "Entry") + orderBy.ToString();
                sqlTxt += ";";

                sqlCmd = new SqlCommand(sqlTxt, sqlCxn);
                sqlCmd.Parameters.AddWithValue("@LT", (_ListName == "" ? 0 : _ListType));
                SqlDataReader sqlRdr = sqlCmd.ExecuteReader();

                while (sqlRdr.Read())
                {
                    if (xListTable.Rows.Count == 0)
                    { // don't want to keep doing this, assuming that this will make the code more efficient.
                        _ListType = (int)sqlRdr["ListType"];
                    }
                    if ((int)sqlRdr["ListIdx"] == 0)
                    {
                        dr = xListTable.NewRow();
                        dr["ListIdx"] = 0;
                        xlTitle = sqlRdr["EntryDescr"].ToString();
                        dr["EntryParam"] = sqlRdr["EntryParm"];
                        dr["EntryValue"] = sqlRdr["EntryValue"];
                        dr["EntryWhen"] = sqlRdr["EntryWhen"];
                        dr["EntryFlag"] = sqlRdr["EntryFlag"];
                        dr["EntryDescr"] = adFns.LeftBrkt + xlTitle + adFns.RghtBrkt;
                        dr["Updated"] = false;
                        dr["Inserted"] = false;
                        xListTable.Rows.Add(dr);
                    }
                    if (Convert.ToInt32(sqlRdr["ListIdx"]) > 0)
                    {
                        string bert = sqlRdr["ListIdx"].ToString() + "] " + sqlRdr["EntryDescr"].ToString();
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
                    }
                }
                sqlRdr.Close();
                if (xlTitle == "") // title was not stored on the table so use the list Name as the default list title.
                {
                    dr = xListTable.NewRow();
                    dr["ListIdx"] = 0;
                    xlTitle = ListName;
                    dr["EntryParam"] = System.DBNull.Value;
                    dr["EntryValue"] = System.DBNull.Value;
                    dr["EntryWhen"] = System.DBNull.Value;
                    dr["EntryFlag"] = System.DBNull.Value;
                    dr["EntryDescr"] = adFns.LeftBrkt + xlTitle + adFns.RghtBrkt;
                    dr["Updated"] = false;
                    dr["Inserted"] = (xListTable.Rows.Count == 0); // if a brand new xlist then init the title???
                    xListTable.Rows.InsertAt(dr, 0);
                }
            }

            if (xListTable.Rows.Count == 0)
            {
                if (autoCreate)
                {
                    Create(initTitle, initValues);
                }
                else
                {
                    _ListName = null;
                    _ListType = -1;
                }
            }

            sqlCxn.Close();
        }
        #endregion

        #region this-stuff
        public Object this[String Descr, Fld fld = Fld.Param]
        {
            get
            {
                Object rc = null;
                String fldName = (fld == Fld.Idx ? "List" : "Entry") + fld.ToString();
                DataRow[] rw = xListTable.Select("EntryDescr='" + Descr + "'");
                if (rw.Length > 0)
                    rc = rw[0][fldName];
                else
                {
                    throw (new Exception(Errors.xList_Entry_Does_Not_Exist.ToString()));
                    //throw (new Exception("xList Entry does not exist"));
                }
                return rc;
            }
            set
            {
                String fldName = (fld == Fld.Idx ? "List" : "Entry") + fld.ToString();
                DataRow[] rw = xListTable.Select("EntryDescr='" + Descr + "'");
                if (rw.Length > 0)
                {
                    rw[0][fldName] = value;
                    rw[0]["Updated"] = true;
                    if (_AutoSave) AutoSave();
                }
            }
        }
        public Object this[Int32 idx, Fld fld = Fld.Descr, Boolean ListIdx = false]
        {
            get
            {
                Object rc = null;
                String fldName = (fld == Fld.Idx ? "List" : "Entry") + fld.ToString();
                if (ListIdx)
                {
                    DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
                    if (rw.Length > 0)
                        rc = rw[0][fldName];
                }
                else
                    rc = xListTable.Rows[idx][fldName];
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
                        if (_AutoSave) AutoSave();
                    }
                }
                else
                {
                    if (value != xListTable.Rows[idx][fldName])
                    {
                        xListTable.Rows[idx][fldName] = value;
                        xListTable.Rows[idx]["Updated"] = true;
                    }
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

        #region built-in methods
        public Boolean Contains(String Descr, Fld fld = Fld.Descr) { return IndexOf(Descr, fld) > 0; }
        public Boolean ContainsTitle { get { return !string.IsNullOrEmpty(this[0].ToString()); } }
        public Int32 Count { get { return ContainsTitle ? xListTable.Rows.Count - 1 : xListTable.Rows.Count; } } // because title is held in row 0!
        public Int32 IndexOf(String Descr, Fld fld = Fld.Descr, Boolean ListIdx = false)
        {
            Int32 rc = 0;
            String fldName = (fld == Fld.Idx ? "List" : "Entry") + fld.ToString();
            DataRow[] rw = xListTable.Select(fldName + "='" + Descr.Replace("'", "''") + "'");
            if (rw.Length > 0)
            {
                if (ListIdx)
                    rc = Convert.ToInt32(rw[0]["ListIdx"]);
                else
                    rc = xListTable.Rows.IndexOf(rw[0]);
            }
            return rc;
        }
        #endregion

        #region xList special methods
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
                    rc = adFns.LeftBrkt + xlTitle + adFns.RghtBrkt;
                //return adFns.LeftBrkt + rc + adFns.RghtBrkt;
                return rc;
            }
            set
            {
                xlTitle = value;
                DataRow[] rw = xListTable.Select("ListIdx=0");
                if (rw.Length > 0)
                {
                    Int32 idx = Convert.ToInt32(rw[0]["ListIdx"]);
                    if (xlTitle.Substring(0, 1) == adFns.LeftBrkt.ToString())
                        xlTitle = xlTitle.Substring(1);
                    if (xlTitle.Substring(xlTitle.Length - 1) == adFns.RghtBrkt.ToString())
                        xlTitle = xlTitle.Substring(0, xlTitle.Length - 2);
                    rw[0]["EntryDescr"] = xlTitle;
                    rw[0]["Updated"] = true;
                    if (_AutoSave) AutoSave();
                }
                else
                { } // do something
                xlTitle = value;
            }
        }
        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="flagPosn">0-7 bit position within flag</param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="value"></param>
        /// <param name="flagPosn">0-7 bit position within flag</param>
        public void SetFlag(Int32 idx, Boolean value, Int32 flagPosn, Boolean ListIdx = false)
        {
            if (ListIdx)
            {
                DataRow[] rw = xListTable.Select("ListIdx=" + idx.ToString());
                if (rw.Length > 0)
                {

                    rw[0]["EntryFlag"] = (value ? (Byte)rw[0]["EntryFlag"] | 1 << flagPosn : (Byte)rw[0]["EntryFlag"] & 255 - (1 << flagPosn));
                    rw[0]["Updated"] = true;
                    if (_AutoSave) AutoSave();
                }
            }
            else
            {
                xListTable.Rows[idx]["EntryFlag"] = (value ? (Byte)xListTable.Rows[idx]["EntryFlag"] | 1 << flagPosn : (Byte)xListTable.Rows[idx]["EntryFlag"] & 255 - (1 << flagPosn));
                xListTable.Rows[idx]["Updated"] = true;
                if (_AutoSave) AutoSave();
            }
        }
        public void SetFlag(String Descr, Boolean value, Int32 flagPosn)
        {
            SetFlag(IndexOf(Descr), value, flagPosn);
        }

        #region general-purpose
        public bool Add(string descr, string param = null, int? value = null, DateTime? when = null, byte? flag = null)
        {
            bool rc = false;
            if (!Contains(descr))
            {
                SqlCommand SQLCmd = new SqlCommand();
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

                SQLCmd.Connection = sqlCxn;
                SQLCmd.CommandText = sqlTxt;
                sqlCxn.Open();
                rc = (SQLCmd.ExecuteNonQuery() == 1);
                sqlCxn.Close();
                if (rc)
                {
                    Append(MaxIdx + 1, descr, param, value, when, flag);
                }
            }
            else
            {
                if (param != null) this[descr] = param;
                if (value != null) this[descr, fld: Fld.Value] = value;
                if (when != null) this[descr, fld: Fld.When] = when;
                if (flag != null) this[descr, fld: Fld.Flag] = flag;
                AutoSave();
            }
            return rc;
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
        #endregion

        private Int32 AutoSave(Int32 rwIdx = -1, Boolean SaveAll = false)
        {   // can I pass in a row idx to save a row rather than having to do the foreach loop as below
            Boolean sqlCxnAlreadyOpen = true;
            Int32 rc = 0;
            String sqlTxt = "";
            SqlCommand sqlCmd = new SqlCommand(sqlTxt, sqlCxn);
            try
            {
                if (sqlCxn.State != ConnectionState.Open)
                {
                    sqlCxn.Open();
                    sqlCxnAlreadyOpen = false;
                }

                if (_ListType == -1)
                {
                    sqlTxt = "Select Max(ListIdx) from xList where ListType=0;";
                    sqlCmd.CommandText = sqlTxt;
                    int.TryParse(sqlCmd.ExecuteScalar().ToString(), out _ListType); ++_ListType;
                    sqlTxt = "Insert xList (ListType,ListIdx,EntryDescr) Values(@LT,@LI,@ED);";
                    sqlCmd.Parameters.Clear();
                    sqlCmd.Parameters.AddWithValue("@LT", 0);
                    sqlCmd.Parameters.AddWithValue("@LI", _ListType);
                    sqlCmd.Parameters.AddWithValue("@ED", _ListName);
                    sqlCmd.CommandText = sqlTxt;
                    sqlCmd.ExecuteNonQuery();
                }

                foreach (DataRow dr in xListTable.Rows)
                {
                    sqlTxt = "";
                    sqlCmd.Parameters.Clear();

                    if (dr["Inserted"] != System.DBNull.Value && Convert.ToBoolean(dr["Inserted"]))
                        sqlTxt = "Insert xList (ListType,ListIdx,EntryDescr,EntryParm,EntryValue,EntryWhen,EntryFlag) Values(@LT,@LI,@ED,@EP,@EV,@EW,@EF);";
                    else if ((dr["Updated"] != System.DBNull.Value && Convert.ToBoolean(dr["Updated"])) || SaveAll) // just incase both flags are set
                        sqlTxt = "Update xList Set EntryDescr=@ED,EntryParm=@EP,EntryValue=@EV,EntryWhen=@EW,EntryFlag=@EF where ListType=@LT and ListIdx=@LI;";

                    if (sqlTxt != "")
                    {
                        sqlCmd.Parameters.AddWithValue("@LT", _ListType);
                        sqlCmd.Parameters.AddWithValue("@LI", dr["ListIdx"]);
                        sqlCmd.Parameters.AddWithValue("@ED", dr["EntryDescr"]);
                        sqlCmd.Parameters.AddWithValue("@EP", dr["EntryParam"]);
                        sqlCmd.Parameters.AddWithValue("@EV", dr["EntryValue"]);
                        sqlCmd.Parameters.AddWithValue("@EW", dr["EntryWhen"]);
                        sqlCmd.Parameters.AddWithValue("@EF", dr["EntryFlag"]);
                        sqlCmd.CommandText = sqlTxt;

                        rc += sqlCmd.ExecuteNonQuery();
                    }
                }
                if (!sqlCxnAlreadyOpen) sqlCxn.Close();
            }
            catch (Exception exc)
            {
                string bert = exc.Message;
            }
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
                    dr["ListIdx"] = -1;
                    foreach (KeyValuePair<Fld, Object> xListEntry in xListRow)
                        dr[(xListEntry.Key == Fld.Idx ? "List" : "Entry") + xListEntry.Key.ToString()] = xListEntry.Value;
                    if ((int)dr["ListIdx"] == -1)
                        dr["ListIdx"] = MaxIdx + 1;
                    dr["Updated"] = true;
                    dr["Inserted"] = true;
                    xListTable.Rows.Add(dr);
                }
            }
            if (_AutoSave) AutoSave(SaveAll: true);
        }

        public void Create(String[] Descr, String[] Param = null, Int32[] Value = null, DateTime[] When = null, Byte[] Flag = null, String initTitle = "")
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

            if (_AutoSave) AutoSave();
        }
        public void Save()
        {
            AutoSave(SaveAll: true);
        }

        #region direct access
        // just in case it is useful to the end user;
        // these end users will only be programmers anyway,
        // I just hope they are of good quality!
        public DataTable rawTable { get { return xListTable; } }
        #endregion

        #region IEnumerator
        public xListRowEntry xListEntry(int idx)
        {
            return new xListRowEntry(xListTable.Rows[idx]);
        }

        public IEnumerator<xListRowEntry> GetEnumerator()
        {
            foreach (DataRow dr in xListTable.Rows)
            {
                // xListRowEntry xre = new xListRowEntry(dr);
                yield return new xListRowEntry(dr);
            }
        }

        

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
            // throw new NotImplementedException();
        }
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            //if (sqlCmd != null)
            //    sqlCmd.Dispose();
            if (sqlCxn != null)
                sqlCxn.Dispose();
            if (xListTable != null)
                xListTable.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
