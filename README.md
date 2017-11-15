# xList
External Lists of simple data forms 

Required:
---------
adDBDefns

Notes:
------
This is not the latest version, but will be updated shortly.

Information:
------------
It was never intended that entries should be deleted from an xList. Example: if an xList 'Titles' contains:
1] Mr.
2] Mrs.
3] Miss

and Mrs. and Miss were deprecated in favour of
4] Ms

then a flag [part of the xList Row Definition] could be set so that the program using this xList should be written to ignore entries with this flag set. If these entries were already refernced by the xList Idx [index] pointer then any records with 2 or 3 would point to a missing entry into the xList.
