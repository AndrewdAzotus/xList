<?php // o
     //
    //   o not sure if I should used a passed $mysqli or if I should create
   //      a new one for each function. Sticking with passed one for now, but
  //       keeping this option open as a future update.
 // >>>> o IMPLEMENT A SAVE UPDATES FUNCTION, NEEDS SOME WAY TO CHECK IF xList
//  >>>>   has been altered SINCE xList opened so other's changes are not lost
//  >>>>   since xList updates are only per page load, if save and cancel are
//  >>>>   to be implemented then all updates will need to be in $_SESSION
//  >>>>   variables.
/* +-------------------------------------------------------------------------+
   | Description:                                                            |
   | Authored by: Andrew J. Davis                                            |
   +-------------------------------------------------------------------------+
   | Modification History:                                                   |
   | 07-05-2015 - Initial version created that reads an xList table entries  |
   | AJD          into an array.                                             |
   | 08-05-2015 - Parm, Value and When entries are now also read and the     |
   | AJD          values made available through functions.                   |
   | 08-05-2015 - Entry, Parm, Value and When entries are modifyable but not |
   | AJD          written to the database                                    |
   | 11-05-2015 - EntryFlag added and select from xList updated to * so that |
   | AJD          if some fields do not exist within a table the code will   |
   |              still work.                                                |
   | 14-05-2015 - Add and init newly created list                            |
   |                                                                         |
   +-------------------------------------------------------------------------+
*/
class xList implements Iterator {
  private $xName;
  private $xType;
  private $xlIdx   = array();
  private $xlDescr = array();
  private $xlParm  = array();
  private $xlValue = array();
  private $xlWhen  = array();
  private $xlFlag  = array();
  private $xlUpdtd = array();

  private $SortOrder = "";
  private $MaxIdx = 0;
  private $Ctr = 0;
  private $SQLCmd;
  private $xmysqli = null;

/* +-------------------------------------------------------------------------+
   xList processing Functions, e.g. construct, destruct, save etc.
*/
  function __construct($ListName = "", $mysqli = null, $autoCreate = False, $initValues = null) {
    $this->xmysqli = $mysqli;
    $this->load($ListName, $autoCreate, $initValues);
  }
///  function __destruct() {
//  // save the xList out to the table, which is fine unless someone else does at the same time.
///  }

//// __get...
//// __set...

  function diags() {
    return "xList is: ".$this->xType."]".$this->xName;
  }


/* +-------------------------------------------------------------------------+
   xList list functions, e.g. maxidx, count and save etc. Note, Load is in
   the construct[or].
*/
  function add($EntryDescr, $EntryParm = null, $EntryValue = null, $EntryWhen = null, $EntryFlag = null) {
//echo "x] add"."<br>";
    $this->MaxIdx++;
    $SQLCmd1 = "Insert xList (ListType,ListIdx,EntryDescr";
    $SQLCmd2 = ") Values(".$this->xType.",".$this->MaxIdx.",'".addslashes($EntryDescr)."'";
    if ($EntryParm != null) {
      $SQLCmd1 = $SQLCmd1.",EntryParm";
      $SQLCmd2 = $SQLCmd2.",'".addslashes($EntryParm)."'";
    }
    if ($EntryValue != null) {
      $SQLCmd1 = $SQLCmd1.",EntryValue";
      $SQLCmd2 = $SQLCmd2.",".$EntryValue;
    }
    if ($EntryWhen != null) {
      $SQLCmd1 = $SQLCmd1.",EntryWhen";
      $SQLCmd2 = $SQLCmd2.",'".$EntryWhen."'";
    }
    if ($EntryFlag != null) {
      $SQLCmd1 = $SQLCmd1.",EntryFlag";
      $SQLCmd2 = $SQLCmd2.",".$EntryFlag;
    }
    $SQLCmd = $SQLCmd1.$SQLCmd2.");";
//echo "add.1]".$SQLCmd."<br>";
    if ($this->xmysqli->query($SQLCmd)) {
      $Idx = $this->MaxIdx;
      $this->xlDescr[$Idx] = $EntryDescr;
      $this->xlParm[$Idx]  = $EntryParm."";
      $this->xlValue[$Idx] = $EntryValue + 0;
      $this->xlWhen[$Idx]  = $EntryWhen;
      $this->xlFlag[$Idx]  = $EntryFlag + 0;
      $this->xlUpdtd[$Idx] = True;
      $this->Ctr++;
    }
  }

  function append($EntryDescr, $EntryParm = null, $EntryValue = null, $EntryWhen = null, $EntryFlag = null) {
//      $Idx = $this->MaxIdx;
    $this->xlDescr[] = $EntryDescr;
    $this->xlParm[]  = $EntryParm."";
    $this->xlValue[] = $EntryValue + 0;
    $this->xlWhen[]  = $EntryWhen;
    $this->xlFlag[]  = $EntryFlag + 0;
    $this->xlUpdtd[] = False;
    $this->Ctr++;
  }

  function count() { return $this->Ctr; }

  function indexOf($FindKey) {
    return array_search($FindKey, $this->xlDescr);
  }
  function indexOfParm($FindKey) {
    return array_search($FindKey, $this->xlParm);
  }
  
  function islocked() {
    $SQLCmd = "Select EntryFlag from xList where ListType=0 and ListIdx=".$this->xType.";";
//echo "islocked.1] ".$SQLCmd."<br>";
    $SQLRes = $this->xmysqli->query($SQLCmd);
    $SQLRow = $SQLRes->fetch_assoc();
    return $SQLRow['EntryFlag'] + 0;
  }
  
  private function load($ListName = "", $autoCreate = False, $initValues = null) { // removed from construct in case
    $this->xType = -1;								  // a reload/refresh is needed.
    if ($ListName == "") {
      $this->xType = 0;
    }
    else {
      $this->xName = $ListName;
      $this->xlIdx[0] = 0;
      $this->xlDescr[0] = $ListName;
      $this->xlUpdtd[0] = false;
      $SQLCmd = "Select * from xList where ListType=0 and EntryDescr='".$this->xName."';";
      $SQLRes = $this->xmysqli->query($SQLCmd);
      if ($SQLRes->num_rows == 0 and $autoCreate == True) {
        $SQLCmd = "Select Max(ListIdx) as MaxIdx from xList where ListType=0;";
        $SQLRes = $this->xmysqli->query($SQLCmd);
        $SQLRow = $SQLRes->fetch_assoc();
        $NewIdx = $SQLRow["MaxIdx"] + 1;
        $SQLCmd = "Insert xList (ListType,ListIdx,EntryDescr) Values(0,".$NewIdx.",'".$ListName."');";
        $SQLRes = $this->xmysqli->query($SQLCmd);
        $this->xType = $NewIdx;
        if (count($initValues) > 0) {
          foreach ($initValues as $Descr) {
            $this->add($Descr);
          }
        }
      }
      elseif ($SQLRes->num_rows > 0) {
        $SQLRow = $SQLRes->fetch_assoc();
        $this->xType = $SQLRow["ListIdx"];
        $this->xlParm[0]  = $SQLRow['EntryParm']."";
        $this->xlValue[0] = $SQLRow['EntryValue'] + 0;
        $this->xlWhen[0]  = $SQLRow['EntryWhen'];
        $this->xlFlag[0]  = $SQLRow['EntryFlag'] + 0;
      }
    }
    if ($this->xType >= 0) {
      $SQLCmd = "Select * from xList where ListType=".$this->xType.";";
      $SQLRes = $this->xmysqli->query($SQLCmd);
      while ($SQLRow = $SQLRes->fetch_assoc()) {
        $Idx = $SQLRow['ListIdx']+0;
        $this->MaxIdx = ($Idx > $this->MaxIdx ? $Idx : $this->MaxIdx);
        $this->xlIdx[$Idx]   = $Idx;			// this may seem odd, but it is for when the xlist is sorted...
        $this->xlDescr[$Idx] = ($Idx == 0 ? $SQLRow['EntryDescr'] : $SQLRow['EntryDescr']);
        $this->xlParm[$Idx]  = $SQLRow['EntryParm']."";
        $this->xlValue[$Idx] = $SQLRow['EntryValue'] + 0;
        $this->xlWhen[$Idx]  = $SQLRow['EntryWhen'];
        $this->xlFlag[$Idx]  = $SQLRow['EntryFlag'] + 0;
        $this->xlUpdtd[$Idx] = False;
        $this->Ctr++;
      }
    }
  }
  
  function lock() {
    $SQLCmd = "Update xList set EntryFlag=True where ListType=0 and ListIdx=".$this->xType.";";
    $this->xmysqli->query($SQLCmd);
  }
  
  function maxidx() { return $this->MaxIdx; }
  
  function save() {
    foreach ($this->xlUpdtd as $Idx => $Updtd) {
      if ($Updtd == True) {
        $SQLCmd = "Update xList set";
        $SQLCmd = $SQLCmd." EntryDescr='".addslashes($this->xlDescr[($Idx)])."'";
        if ($this->xlParm[($Idx)]  != null) $SQLCmd = $SQLCmd.",EntryParm='".addslashes($this->xlParm[($Idx)])."'";
        if ($this->xlValue[($Idx)] != null) $SQLCmd = $SQLCmd.",EntryValue=".$this->xlValue[($Idx)];
        if ($this->xlWhen[($Idx)]  != null) {
          $SQLCmd = $SQLCmd.",EntryWhen='".$this->xlWhen[($Idx)]."'";
        }
        if ($this->xlFlag[($Idx)]  != null) $SQLCmd = $SQLCmd.",EntryFlag=".$this->xlFlag[($Idx)];
        $SQLCmd = $SQLCmd." where ListType=".$this->xType." and ListIdx=".$this->xlIdx[($Idx)];
        $this->xmysqli->query($SQLCmd);
      }
    }
  }
  
  function sort($OrderBy = null) { // used when a different foreach order is required...
    switch ($OrderBy) {
    case "Parm":  $SortBy = $this->xlParm; break;
    case "Value": $SortBy = $this->xlValue; break;
    case "When":  $SortBy = $this->xlWhen; break;
    case "Flag":  $SortBy = $this->xlFlag; break;
    case "Index": $SortBy = $this->xlIdx; break;
    default:      $SortBy = $this->xlDescr;
    }
    $SortBy[0] = "            ".$SortBy[0]; // to force title to the top, twelve spaces should be enough
    array_multisort($SortBy,
                    $this->xlDescr, 
                    $this->xlParm, 
                    $this->xlValue, 
                    $this->xlWhen, 
                    $this->xlFlag, 
                    $this->xlIdx, $this->xlUpdtd);
  }
  
  function unlock($xList = null) { // potential override for unlocking an alternate xList;
    if ($xList == null) {
      $SQLCmd = "Update xList set EntryFlag=False where ListType=0 and ListIdx=".$this->xType.";";
//echo "unlock.1] ".$SQLCmd."<br>";
      $this->xmysqli->query($SQLCmd);
    }
    else {
      $SQLCmd = "Update xList set EntryFlag=False where ListType=0 and EntryDescr='".$xList."';";
//echo "unlock.2] ".$SQLCmd."<br>";
      $this->xmysqli->query($SQLCmd);
    }
  }
  
/* +-------------------------------------------------------------------------+
   xList set and get functions, e.g. entry, parm, value, when etc.
*/
  function entry($Idx = -1, $Updt = null) {
    if ($Idx > -1 && $Updt !== null) {
      $this->xlDescr[($Idx+0)] = $Updt;
      $this->xlUpdtd[($Idx+0)] = True;
    }
    return ($Idx > -1 ? $this->xlDescr[($Idx+0)] : ""); //[=]<<
//    return ($Idx > -1 ? $this->xlDescr[$[=]<<
  }
  function parm($Idx = -1, $Updt = null) {
    if ($Idx > -1 && $Updt !== null) {
      $this->xlParm[($Idx+0)] = $Updt;
      $this->xlUpdtd[($Idx+0)] = True;
    }
//echo ($Idx+0)."<br>";
//foreach ($this->xlParm as $kk => $vv) {echo "[$kk .. $vv] ";}
//echo $this->xlIdx[($Idx+0)]."<br>";
//echo $this->xlDescr[$this->xlIdx[($Idx+0)]]."<br>";
//echo $this->xlParm[$this->xlIdx[($Idx+0)]]."<br>";
//echo $this->xlDescr[($Idx+0)]."<br>";[=]
    return ($Idx > -1 ? $this->xlParm[($Idx+0)] : "");
  }
  function eparm($entry = "", $Updt = null) {
    return ($entry == "" ? "" : $this->xlParm[array_search($entry, $this->xlDescr)]);
  }
  function value($Idx, $Updt = null) {
    if ($Idx > -1 && $Updt !== null) {
      $this->xlValue[($Idx+0)] = $Updt;
      $this->xlUpdtd[($Idx+0)] = True;
    }
    return ($Idx > -1 ? $this->xlValue[($Idx+0)] : "");
  }
  function when($Idx, $Updt = null) {
    if ($Idx > -1 && $Updt !== null) {
      $this->xlWhen[($Idx+0)] = $Updt;
      $this->xlUpdtd[($Idx+0)] = True;
    }
    return ($Idx > -1 ? $this->xlWhen[($Idx+0)] : "");
  }
  function flag($Idx, $Updt = null) {
    if ($Idx > -1 && $Updt !== null) {
      $this->xlFlag[($Idx+0)] = $Updt;
      $this->xlUpdtd[($Idx+0)] = True;
    }
    return ($Idx > -1 ? $this->xlFlag[($Idx+0)] : "");
  }
  
  function title($Updt = null)	{
    return $this->entry(0, $Updt);
  }
/* +-------------------------------------------------------------------------+
   Iterator functions
*/
  // Required definition of interface IteratorAggregate
  public function current() {
    return current($this->xlDescr);
  }
//  public function key()     { return $this->xlIdx[key($this->xlDescr)]; }
  public function key()     { return key($this->xlDescr); }
  public function next()    { return next($this->xlDescr); }
  public function rewind(){
    reset($this->xlDescr);
    if ($this->xlDescr[0] != "") {
      next($this->xlDescr);	// called so that the foreach iterator skips the title which is stored in key=0
    }
  }
  public function valid()
  {
    $key = key($this->xlDescr);
    return ($key !== NULL && $key !== FALSE);
  }

/*
  // Required definition of interface IteratorAggregate
  public function current() {
    return current($this->xlIdx);
//    return $this->xlDescr[current($this->xlIdx)];
  }
  public function key()     { return key($this->xlIdx); }
  public function next()    { return next($this->Idx); }
//  public function next()    { return $this->xlDescr[next($this->Idx)]; }
  public function rewind(){
    reset($this->xlIdx);
    if ($this->xlDescr[0] != "") {
      next($this->xlIdx);	// called so that the foreach iterator skips the title which is stored in key=0
    }
  }
  public function valid()
  {[=]*/
}
?>
