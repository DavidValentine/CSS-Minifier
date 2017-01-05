using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSS_Minifier
{
    /// <summary>
    /// CSS class
    /// </summary>
    public class CascadingStyle
    {
        public string Name { set; get; }
        public StyleCollection Styles { set; get; }

        public CascadingStyle()
        {
            Styles = new StyleCollection();
        }
    }

    /// <summary>
    /// CSS collection
    /// </summary>
    public class CascadingStyleCollection : List<CascadingStyle>
    {
        public string Name { set; get; }

        
    }
}
