using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpendingReport.DataContext
{
    public sealed class DbParameter
    {
        public string Name
        {
            get;
            private set;
        }

        public object Value
        {
            get;
            private set;
        }

        public DbParameter(string name, object value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}