#define MSSQL
#if (MSSQL == false)
#define MySQL
#endif

namespace azotus
{
  public class Defns
  {
    public static readonly String LeftBrkt = ((char)171).ToString();
    public static readonly String RghtBrkt = ((char)187).ToString();
    
    private string _dbName = null;
    private string _dbUser = null;
    private string _dbPswd = null;
    private string _dbSrvr = null;
    Int16 _dbPort = -1;                         // only used for Mysql

    public Defns(
      string dbName = "Common",
      string dbUser = "",
      string dbPswd = "",
      string dbSrvr = "localhost",
#if MSSQL
      Int16 dbPort = -1
#endif
#if MySQL
      Int16 dbPort = 3306
#endif
      )
    {
      _dbName = dbName;
      _dbUser = dbUser;
      _dbPswd = dbPswd;
      _dbSrvr = dbSrvr;
      _dbPort = dbPort;
    }
  }
  
  public String CxnDtls()
  {
    string strCxn = "";

    strCxn += $"database={_dbName};";
    strCxn += "user=" + _dbUser + ";";
    strCxn += "password=" + _dbPswd + ";";
    strCxn += "server=" + _dbSrvr + ";";
    if (_dbPort >= 0)
      strCxn += "port=" + _dbPort.ToString() + ";";
    return strCxn;
  }
  
  public string CxnDtls(string databaseName)
  {
    if (databaseName == "") { databaseName = _dbName; }
    string strCxn = "";
    if (databaseName != "")
    {
      strCxn += "database=" + databaseName + ";";
      strCxn += "user=" + _dbUser + ";";
      strCxn += "password=" + _dbPswd + ";";
      strCxn += "server=" + _dbSrvr + ";";
      if (_dbPort >= 0)
        strCxn += "port=" + _dbPort.ToString() + ";";
    }
    return strCxn;
  }
}
