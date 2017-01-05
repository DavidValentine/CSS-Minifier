using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSS_Minifier
{
    /// <summary>
    /// CSS Style Class
    /// </summary>
    public class Style
    {
        public string Name { set; get; }
        public string Setting { set; get; }

        public override bool Equals(object obj)
        {
            bool bReturn = false;
            if (obj.GetType() == this.GetType())
            {
                Style sTemp = (Style)obj;
                if (sTemp.Name == Name && sTemp.Setting == Setting)
                { bReturn = true; }
            }
            return bReturn;
        }
    }

    /// <summary>
    /// CSS Style Collection
    /// </summary>
    public class StyleCollection : List<Style>
    {
        public override bool Equals(object obj)
        {
            bool bReturn = false;
            if (obj.GetType() == this.GetType())
            {
                StyleCollection scTemp = (StyleCollection)obj;
                if (scTemp.Count() == this.Count())
                {
                    for(int i = 0, l = this.Count(); i < l; i++)
                    {
                        if(!scTemp[i].Equals(this[i]))
                        {break;}
                        if (i + 1 == l)
                        { bReturn = true; }
                    }
                }
            }
            return bReturn;
        }
    }
}
