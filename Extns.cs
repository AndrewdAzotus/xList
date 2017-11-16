using System.Windows.Controls.Primitives;

namespace Azotus
{
    public static partial class Extns
    {
        public static void Populate(this xList fldData, Selector frmField, int chosenIdx = -1, bool useTitle = true, bool inclParmData = false)
        {
            int selIdx = chosenIdx;
            if (useTitle)
                frmField.Items.Add(fldData.Title);

            for (int Lp1 = 1; Lp1 <= fldData.MaxIdx; ++Lp1)
            {
                string thing = fldData[Lp1, xList.Fld.Descr].ToString();
                if (fldData[Lp1, xList.Fld.Idx].Equals(chosenIdx)) selIdx = Lp1;
                if (inclParmData) thing += " - " + fldData[Lp1, xList.Fld.Param] + "[" + fldData[Lp1, xList.Fld.Idx] + "]";
                frmField.Items.Add(thing);
            }

            if (selIdx >= 0)
                frmField.SelectedIndex = selIdx;
        }
    }
}
