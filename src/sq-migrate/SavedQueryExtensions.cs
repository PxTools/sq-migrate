using PCAxis.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sq_migrate
{
    public static class SavedQueryExtensions
    {
        public static int NumberOfPerPart(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "PER_PART");
        }

        public static int NumberOfChangeValueOrder(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "CHANGE_VALUE_ORDER");
        }

        public static int NumberOfDeleteValue(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "DELETE_VALUE");
        }
        public static int NumberOfDeleteVariable(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "DELETE_VARIABLE");
        }

        public static int NumberOfSortTime(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "SORT_TIME");
        }
        public static int NumberOfSplitTime(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "SPLIT_TIME");
        }

        public static int NumberOfSum(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "SUM");
        }

        public static int NumberOfChangeDecimals(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "CHANGE_DECIMALS");
        }

        public static int NumberOfChangeText(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "CHANGE_TEXT");
        }

        public static int NumberOfPivotTimeToHeading(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "PIVOT_TIME_TO_HEADING");
        }

        public static int NumberOfChangeCodeTextPresentation(this SavedQuery sq)
        {
            return sq.Workflow.Count(s => s.Type == "CHANGE_TEXT_CODE_PRESENTATION");
        }
    }
}
