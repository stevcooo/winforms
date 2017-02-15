using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseTools
{
    public class RefferencedTable
    {
        public String ParentTableName { get; set; }
        public String TableName { get; set; }
        public String ColumnName { get; set; }
        public String SchemaName { get; set; }
        public Boolean IsProcessed { get; set; }
        public Boolean IsParent { get; set; }
    }
}
