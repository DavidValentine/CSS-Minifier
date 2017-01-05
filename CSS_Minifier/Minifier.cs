using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CSS_Minifier
{
    /// <summary>
    /// This class parses the CSS text and performs all of the minifying actions against the CSS Style Collection
    /// </summary>
    internal class Minifier
    {
        private string strLogText = string.Empty;

        internal bool bWriteLog {set; get;}
        internal string strLog { get { return strLogText; } }

        /// <summary>
        /// Processes the CSS string 
        /// </summary>
        /// <param name="strIncoming">Incoming CSS</param>
        internal string processCSS(string strIncoming)
        {
            CascadingStyleCollection cscMinified = new CascadingStyleCollection();
            //remove formatting
            strIncoming = Regex.Replace(strIncoming, @"\t|\n|\r", string.Empty);
            strIncoming = strIncoming.Replace("; ", ";").Replace(", ", ",").Replace(": ", ":").Replace("'", "").Replace("'", "").Replace("\"", "").Replace("\"", "");

            //Remove comments
            strIncoming = Regex.Replace(strIncoming, @"(/\*)+((?!\*/).)*(\*/)+", string.Empty);

            //split into array. Split on ending bracket of css class.
            string[] arrCSSBlock = parseClasses(strIncoming); //Regex.Split(text, @"(?<=(\{.*\}))");

            //parse styles
            foreach (string str in arrCSSBlock)
            {
                if (str.Count(c => c == '{') <= 1)
                { cscMinified.AddRange(parseCS(str)); }
                else
                {
                    if (bWriteLog)
                    { strLogText += str + Environment.NewLine; }
                }
            }
            
            if (bWriteLog)
            { strLogText += "Begin minimizing css" + Environment.NewLine; }

            //remove duplicate classes
            removeDupeClasses(cscMinified);
            removeDupeStyles(cscMinified);

            //minimize styles
            for (int i = 0, l = cscMinified.Count; i < l; i++)
            {
                if (cscMinified[i].Styles.Count > 0)
                { minStyles(cscMinified[i].Styles); }
                else
                {
                    if (bWriteLog)
                    { strLogText += string.Format("Removed empty css class. [{0}]", cscMinified[i].Name) + Environment.NewLine; }
                    cscMinified.Remove(cscMinified[i]);
                    l--;
                    i--;
                }
            }

            if (bWriteLog)
            { strLogText += "Completed minimizing css" + Environment.NewLine; }

            //Look for common styles
            reorganizeSelectors(cscMinified);

            //remove newly created empty styles
            for (int i = 0, l = cscMinified.Count; i < l; i++)
            {
                if (cscMinified[i].Styles.Count <= 0)
                { 
                    cscMinified.Remove(cscMinified[i]);
                    l--;
                    i--;
                }
            }

            //Build the new css file
            StringBuilder sbCssFile = new StringBuilder();
            string strCssName = string.Empty;
            for (int i = 0, l = cscMinified.Count; i < l; i++)
            {
                string strCssStyle = string.Empty;
                strCssName += cscMinified[i].Name + ",";

                if (cscMinified[i].Name.Contains(".topTableRef"))
                { cscMinified[i].Name.ToString(); }

                if (i < l && (i + 1 == l || !cscMinified[i].Styles.Equals(cscMinified[i + 1].Styles)))
                {
                    foreach (Style css in cscMinified[i].Styles)
                    {
                        strCssStyle += string.Format("{0}:{1};", css.Name.Trim(), css.Setting.Trim());
                    }
                    if (strCssStyle.EndsWith(";"))
                    { strCssStyle = strCssStyle.Substring(0, strCssStyle.Length - 1); }

                    sbCssFile.Append(string.Format("{0}{{{1}}}", strCssName.Remove(strCssName.LastIndexOf(","), 1).Trim(), strCssStyle.Trim()));
                    strCssName = string.Empty;
                }
            }
            return sbCssFile.ToString().Trim();
        }

        /// <summary>
        /// Function parses the CSS file into an array for each entry
        /// </summary>
        /// <param name="strIncoming">CSS File Contents</param>
        /// <returns>string array</returns>
        private string[] parseClasses(string strIncoming)
        {
            if (bWriteLog)
            { strLogText += "Begin parsing css" + Environment.NewLine; }

            List<string> lstReturn = new List<string>();

            int iBracket = strIncoming.IndexOf("}") + 1;
            do
            {
                if (

                    strIncoming.Substring(0, iBracket).Count(c => c == '{') ==
                    strIncoming.Substring(0, iBracket).Count(c => c == '}')
                  )
                {
                    lstReturn.Add(strIncoming.Substring(0, iBracket));
                    strIncoming = strIncoming.Substring(iBracket);
                    iBracket = 0;

                    //strLogText += "Remaining CSS: " + strIncoming.Length + Environment.NewLine;
                }

                iBracket = strIncoming.IndexOf("}", iBracket) + 1;
            }
            while (strIncoming.Length > 0);
            if (bWriteLog)
            { strLogText += "Parsing Complete!" + Environment.NewLine; }
            return lstReturn.ToArray();
        }

        /// <summary>
        /// Function parses styles
        /// </summary>
        /// <param name="strIncoming">string of classes and styles</param>
        /// <returns>collection of cascading styles</returns>
        private CascadingStyleCollection parseCS(string strIncoming)
        {
            CascadingStyleCollection cscReturn = new CascadingStyleCollection();

            strIncoming = strIncoming.Remove(strIncoming.LastIndexOf("}"));
            string[] strCSSClass = strIncoming.Split('{');

            foreach (string strClassName in strCSSClass[0].Split(','))
            {
                string[] strStyles = strCSSClass[1].Trim().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                StyleCollection scTemp = new StyleCollection();
                scTemp.AddRange(strStyles.Select(str => new Style() { Name = str.Split(':')[0].Trim(), Setting = str.Split(':')[1].Trim() }).Distinct().ToList());

                CascadingStyle cs = new CascadingStyle() { Name = strClassName.Trim(), Styles = scTemp };
                cscReturn.Add(cs);
            }



            return cscReturn;
        }

        /// <summary>
        /// Minimizes a collection of styles
        /// </summary>
        /// <param name="lcsIncoming">List of Styles</param>
        private void minStyles(StyleCollection lcsIncoming)
        {
            for (int i = 0, l = lcsIncoming.Count(); i < l; i++)
            {
                //Fixes double .. typo (Ex: 1..05em)
                foreach (Match match in Regex.Matches(lcsIncoming[i].Setting, @"[\d]+[.]{2}[\d]+"))
                { lcsIncoming[i].Setting = lcsIncoming[i].Setting.Replace("..", "."); }

                //format numeric values -- Exclude Hex Codes
                foreach (Match match in Regex.Matches(lcsIncoming[i].Setting, @"( |^)+([\d]+[\.]?[\d]*)+"))
                {
                    string strTemp = decimal.Parse(match.Value).ToString("#.###");
                    lcsIncoming[i].Setting = lcsIncoming[i].Setting.Replace(match.Value.Trim(), String.IsNullOrWhiteSpace(strTemp) ? "0" : strTemp);
                }

                //replace em with % if it has a decimal -- Exclude Height and Width
                if (lcsIncoming[i].Name.ToLower() != "width" && lcsIncoming[i].Name.ToLower() != "height")
                {
                    foreach (Match match in Regex.Matches(lcsIncoming[i].Setting, @"( |^)+([\d]+[.]{1}[\d]+em)+"))
                    {
                        string strTemp = decimal.Parse(match.Value.Replace("em", string.Empty)).ToString("####%");
                        lcsIncoming[i].Setting = lcsIncoming[i].Setting.Replace(match.Value, String.IsNullOrWhiteSpace(strTemp) ? "0" : strTemp);
                    }

                    //replace % with em if it is divible by 100
                    foreach (Match match in Regex.Matches(lcsIncoming[i].Setting, @"([\d]+(00)+%)+"))
                    {
                        string strTemp = (decimal.Parse(match.Value.Replace("%", string.Empty)) / 100).ToString("####em");
                        lcsIncoming[i].Setting = lcsIncoming[i].Setting.Replace(match.Value, String.IsNullOrWhiteSpace(strTemp) ? "0" : strTemp);
                    }
                }

                //Optimize for zero
                foreach (Match match in Regex.Matches(lcsIncoming[i].Setting, @"( |^)+((0.)?((0px)|(0em)|(0%)))+"))
                {
                    string[] strSetting = lcsIncoming[i].Setting.Split(' ');
                    for (int x = 0, z = strSetting.Length; x < z; x++)
                    {
                        if (strSetting[x] == match.Value.Trim())
                        { strSetting[x] = "0"; }
                    }

                    lcsIncoming[i].Setting = string.Join(" ", strSetting).Trim();

                }

                //Optimize for bold
                foreach (Match match in Regex.Matches(lcsIncoming[i].Name, @"((font-weight)|(font))+"))
                {
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting, @"(bold)+", "700");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting, @"(normal)+", "400");
                }
                //Optimize colors
                foreach (Match match in Regex.Matches(lcsIncoming[i].Setting.ToLower(), @"( |^)+(black)|(white)|(yellow)|(green)|(aliceblue)|(antiquewhite)|(aquamarine)|(blanchedalmond)
                        |(blueviolet)|(burlywood)|(cadetblue)|(chartreuse)|(chocolate)|(cornflowerblue)|(cornsilk)|(darkblue)|(darkcyan)|(darkgoldenrod)|(darkgray)|(darkgreen)|(darkkhaki)|(darkmagenta)
                        |(darkolivegreen)|(darkorange)|(darkorchid)|(darksalmon)|(darkseagreen)|(darkslateblue)|(darkslategray)|(darkturquoise)|(darkviolet)|(deeppink)|(deepskyblue)|(dodgerblue)
                        |(firebrick)|(floralwhite)|(forestgreen)|(fuchsia)|(gainsboro)|(ghostwhite)|(goldenrod)|(greenyellow)|(honeydew)|(indianred)|(lavender)|(lavenderblush)|(lawngreen)|(lemonchiffon)
                        |(lightblue)|(lightcoral)|(lightcyan)|(lightgoldenrodyellow)|(lightgray)|(lightgreen)|(lightpink)|(lightsalmon)|(lightseagreen)|(lightskyblue)|(lightslategray)|(lightsteelblue)|(lightyellow)|(limegreen)|(magenta)|(mediumaquamarine)
                        |(mediumblue)|(mediumorchid)|(mediumpurple)|(mediumseagreen)|(mediumslateblue)|(mediumspringgreen)|(mediumturquoise)|(mediumvioletred)|(midnightblue)|(mintcream)|(mistyrose)
                        |(moccasin)|(navajowhite)|(olivedrab)|(orangered)|(palegoldenrod)|(palegreen)|(paleturquoise)|(palevioletred)|(papayawhip)|(peachpuff)|(powderblue)|(rosybrown)|(royalblue)
                        |(saddlebrown)|(sandybrown)|(seagreen)|(seashell)|(slateblue)|(slategray)|(springgreen)|(steelblue)|(turquoise)|(whitesmoke)|(yellowgreen)+"))
                {


                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(aliceblue)+", "#f0f8ff");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(antiquewhite)+", "#faebd7");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(aquamarine)+", "#7fffd4");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(black)+", "#000");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(blanchedalmond)+", "#ffebcd");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(blueviolet)+", "#8a2be2");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(burlywood)+", "#deb887");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(cadetblue)+", "#5f9ea0");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(chartreuse)+", "#7fff00");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(chocolate)+", "#d2691e");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(cornflowerblue)+", "#6495ed");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(cornsilk)+", "#fff8dc");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkblue)+", "#00008b");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkcyan)+", "#008b8b");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkgoldenrod)+", "#b8860b");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkgray)+", "#a9a9a9");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkgreen)+", "#006400");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkkhaki)+", "#bdb76b");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkmagenta)+", "#8b008b");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkolivegreen)+", "#556b22");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkorange)+", "#ff8c00");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkorchid)+", "#9932cc");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darksalmon)+", "#e9967a");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkseagreen)+", "#8fbc8f");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkslateblue)+", "#483d8b");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkslategray)+", "#2f4f4f");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkturquoise)+", "#00ced1");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(darkviolet)+", "#9400d3");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(deeppink)+", "#ff1493");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(deepskyblue)+", "#00bfff");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(dodgerblue)+", "#1e90ff");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(firebrick)+", "#b22222");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(floralwhite)+", "#fffaf0");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(forestgreen)+", "#228b22");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(fuchsia)+", "#f0f");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(gainsboro)+", "#dcdcdc");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(ghostwhite)+", "#f8f8ff");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(goldenrod)+", "#daa520");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(green)+", "#0f0");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(greenyellow)+", "#adff2f");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(honeydew)+", "#f0fff0");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(indianred)+", "#cd5c5c");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lavender)+", "#e6e6fa");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lavenderblush)+", "#fff0f5");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lawngreen)+", "#7cfc00");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lemonchiffon)+", "#fffacd");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightblue)+", "#add8e6");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightcoral)+", "#f08080");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightcyan)+", "#e0ffff");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightgoldenrodyellow)+", "#fafad2");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightgray)+", "#d3d3d3");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightgreen)+", "#90ee90");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightpink)+", "#ffb6c1");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightsalmon)+", "#ffa07a");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightseagreen)+", "#20b2aa");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightskyblue)+", "#87cefa");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightslategray)+", "#789");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightsteelblue)+", "#b0c4de");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(lightyellow)+", "#ffffe0");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(limegreen)+", "#32cd32");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(magenta)+", "#f0f");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mediumaquamarine)+", "#66cdaa");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mediumblue)+", "#0000cd");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mediumorchid)+", "#ba55d3");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mediumpurple)+", "#9370db");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mediumseagreen)+", "#3cb371");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mediumslateblue)+", "#7b68ee");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mediumspringgreen)+", "#00fa9a");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mediumturquoise)+", "#48d1cc");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mediumvioletred)+", "#c71585");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(midnightblue)+", "#191970");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mintcream)+", "#f5fffa");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(mistyrose)+", "#ffe4e1");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(moccasin)+", "#ffe4b5");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(navajowhite)+", "#ffdead");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(olivedrab)+", "#6b8e23");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(orangered)+", "#ff4500");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(palegoldenrod)+", "#eee8aa");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(palegreen)+", "#98fb98");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(paleturquoise)+", "#afeeee");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(palevioletred)+", "#db7093");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(papayawhip)+", "#ffefd5");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(peachpuff)+", "#ffdab9");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(powderblue)+", "#b0e0e6");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(rosybrown)+", "#bc8f8f");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(royalblue)+", "#4169e1");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(saddlebrown)+", "#8b4513");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(sandybrown)+", "#f4a460");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(seagreen)+", "#2e8b57");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(seashell)+", "#fff5ee");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(slateblue)+", "#6a5acd");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(slategray)+", "#708090");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(springgreen)+", "#00ff7f");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(steelblue)+", "#4682b4");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(turquoise)+", "#40e0d0");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(white)+", "#fff");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(whitesmoke)+", "#f5f5f5");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(yellow)+", "#ff0");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(yellowgreen)+", "#9acd32");
                }

                //Optimize colors Hex Codes
                foreach (Match match in Regex.Matches(lcsIncoming[i].Setting.ToLower(), @"( |^)+(#FF0000)|(808080)|(ffd700)|(fffff0)|(f0e68c)|(faf0e6)|(000080)|(808000)
                        |(da70d6)|(cd853f)|(ffc0cb)|(dda0dd)|(800080)|(ff0000)|(fa8072)|(a0522d)|(c0c0c0)|(fffafa)|(d2b48c)|(008080)|(ff6347)|(f5deb3)+"))
                {
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#808080)+", "gray");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#ffd700)+", "gold");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#fffff0)+", "ivory");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#f0e68c)+", "khaki");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#faf0e6)+", "linen");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#000080)+", "navy");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#808000)+", "olive");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#da70d6)+", "orchid");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#cd853f)+", "peru");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#ffc0cb)+", "pink");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#dda0dd)+", "plum");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#800080)+", "purple");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#ff0000)+", "red");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#fa8072)+", "salmon");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#a0522d)+", "sienna");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#c0c0c0)+", "silver");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#fffafa)+", "snow");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#d2b48c)+", "tan");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#008080)+", "teal");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#ff6347)+", "tomato");
                    lcsIncoming[i].Setting = Regex.Replace(lcsIncoming[i].Setting.ToLower(), @"(#f5deb3)+", "wheat");
                }

                foreach (Match match in Regex.Matches(lcsIncoming[i].Setting, @"( |^)+((#[0-9A-Fa-f]{6}))+"))
                {
                    string strHex = match.Value.Trim().Replace("#", string.Empty);
                    if (strHex.Substring(5, 1).Equals(strHex.Substring(4, 1)) && strHex.Substring(3, 1).Equals(strHex.Substring(2, 1)) && strHex.Substring(1, 1).Equals(strHex.Substring(0, 1)))
                    {
                        strHex = strHex.Substring(0, strHex.Length - 1);
                        strHex = strHex.Remove(3, 1);
                        strHex = strHex.Remove(2, 1);
                    }
                    lcsIncoming[i].Setting = lcsIncoming[i].Setting.Replace(match.Value.Trim(), string.Format("#{0}", strHex));
                }
            }

            //Optimize padding
            minimizeCardinalStyles(lcsIncoming, "padding");
            //Optimize margin
            minimizeCardinalStyles(lcsIncoming, "margin");
            //Optimize radius
            minimizeCardinalStyles(lcsIncoming, "radius");

            //Optimize border
            minimizeBorderStyles(lcsIncoming);
            //Optimize font
            minimizeFontStyles(lcsIncoming);
            //Optimize background
            minimizeBackgroundStyles(lcsIncoming);
        }

        /// <summary>
        /// Function removes duplicate styles
        /// </summary>
        /// <param name="cscIncoming">Max CascadingStyleCollection</param>
        /// <returns>Min CascadingStyleCollection</returns>
        private void removeDupeStyles(CascadingStyleCollection cscIncoming)
        {
            foreach (CascadingStyle cs in cscIncoming)
            {
                for (int i = 0, l = cs.Styles.Count(); i < l; i++)
                {
                    //remove duplicate style definitions
                    if (cs.Styles.Count(s => s.Name == cs.Styles[i].Name) > 1)
                    {
                        if (bWriteLog)
                        { strLogText += string.Format("Removed duplicate style; {0} from css class; {1}", cs.Styles[i].Name, cs.Name) + Environment.NewLine; }
                        cs.Styles.Remove(cs.Styles[i]);
                        i--;
                        l--;
                    }

                    //check for shorthand
                }
            }
        }

        /// <summary>
        /// This function removes duplicate classes and flattens the CSS class if they are duplicated
        /// </summary>
        /// <param name="cscIncoming">Messy CascadingStyleCollection</param>
        private void removeDupeClasses(CascadingStyleCollection cscIncoming)
        {
            string[] duplKeys = cscIncoming.GroupBy(csc => csc.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();

            foreach (string strKey in duplKeys)
            {
                var test = cscIncoming.Where(csc => csc.Name == strKey).ToList();

                CascadingStyleCollection duplicates = new CascadingStyleCollection();
                duplicates.AddRange(cscIncoming.Where(csc => csc.Name == strKey).ToList());

                for (int i = 1, l = duplicates.Count(); i < l; i++)
                {
                    foreach (Style kv in duplicates[i].Styles)
                    {
                        //only process non matching styles
                        if (!duplicates[0].Styles.Contains(kv))
                        {
                            //if style exists overwrite it
                            if (duplicates[0].Styles.Where(s => s.Name == kv.Name).Count() > 0)
                            {
                                var tempStyle = duplicates[0].Styles.Where(s => s.Name == kv.Name).First();
                                tempStyle.Setting = kv.Setting;
                                duplicates[0].Styles.Remove(duplicates[0].Styles.Where(s => s.Name == kv.Name).First());
                                duplicates[0].Styles.Add(tempStyle);
                            }
                            else
                            {
                                duplicates[0].Styles.Add(kv);
                            }

                        }
                    }

                    if (bWriteLog)
                    { strLogText += string.Format("Flattened duplicate css class; {0}", duplicates[i].Name) + Environment.NewLine; }
                    cscIncoming.Remove(duplicates[i]);
                }
            }
        }

        /// <summary>
        /// Optimizes any style that has a cardinal short hand
        /// </summary>
        /// <param name="lcsIncoming">style collection</param>
        /// <param name="cardinalStyleName">style name</param>
        /// <example>padding:top right bottom left</example>
        private void minimizeCardinalStyles(StyleCollection lcsIncoming, string cardinalStyleName)
        {
            //Optimize padding
            Style[] cssMatches = lcsIncoming.Where(s => s.Name.ToLower().StartsWith(cardinalStyleName) == true).ToArray();
            if (cssMatches.Length > 0)
            {
                string top = string.Empty, right = string.Empty, bottom = string.Empty, left = string.Empty;
                for (int i = 0, l = cssMatches.Length; i < l; i++)
                {
                    if (cssMatches[i].Name.ToLower() == cardinalStyleName.ToLower())
                    {
                        string[] trbl = cssMatches[i].Setting.Split(' ');

                        if (trbl.GetUpperBound(0) == 0)
                        {
                            top = trbl[0].Trim();
                            right = trbl[0].Trim();
                            bottom = trbl[0].Trim();
                            left = trbl[0].Trim();
                        }
                        else
                        {
                            top = trbl[0].Trim();
                            right = (trbl.GetUpperBound(0) >= 1) ? trbl[1].Trim() : string.Empty;
                            bottom = (trbl.GetUpperBound(0) >= 2) ? trbl[2].Trim() : string.Empty;
                            left = (trbl.GetUpperBound(0) >= 3) ? trbl[3].Trim() : string.Empty;
                        }
                    }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-top", cardinalStyleName))
                    { top = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-right", cardinalStyleName))
                    { right = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-bottom", cardinalStyleName))
                    { bottom = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-left", cardinalStyleName))
                    { left = cssMatches[i].Setting.Trim(); }
                }

                if (!String.IsNullOrWhiteSpace(top) && !String.IsNullOrWhiteSpace(right))
                {
                    Style cssShortHand = null;

                    if (top == bottom && top == right && right == left)
                    { cssShortHand = new Style() { Name = cardinalStyleName, Setting = string.Format("{0}", top) }; }
                    else if (top == bottom && right == left)
                    { cssShortHand = new Style() { Name = cardinalStyleName, Setting = string.Format("{0} {1}", top, right) }; }
                    else if (right == left)
                    { cssShortHand = new Style() { Name = cardinalStyleName, Setting = string.Format("{0} {1} {2}", top, right, bottom) }; }
                    else
                    { cssShortHand = new Style() { Name = cardinalStyleName, Setting = string.Format("{0} {1} {2} {3}", top, right, bottom, left) }; }
                    Regex.Replace(cssShortHand.Setting, @"[ ]{2,}", @" ");

                    //if there is more than one or the shorthand version is shorter, then convert to short hand.
                    if (cssMatches.Length > 1 || string.Format("{0}{1}", cssShortHand.Name, cssShortHand.Setting).Length < string.Format("{0}{1}", lcsIncoming[0].Name, lcsIncoming[0].Setting).Length)
                    {
                        //Delete all current matches from collection
                        lcsIncoming.RemoveAll(css => css.Name.ToLower().StartsWith(cardinalStyleName) == true);
                        //Add shorthand.
                        lcsIncoming.Add(cssShortHand);
                    }
                }
            }
        }

        /// <summary>
        /// Optimizes font styles
        /// </summary>
        /// <param name="lcsIncoming">style collection</param>
        /// <example>font:font-style font-variant font-weight font-size/line-height font-family</example>
        private void minimizeFontStyles(StyleCollection lcsIncoming)
        {
            //Optimize
            string shortStyleName = "font";
            Style[] cssMatches = lcsIncoming.Where(s => s.Name.ToLower() == "font" || s.Name.ToLower() == "font-style" || s.Name.ToLower() == "font-variant" || s.Name.ToLower() == "font-weight" || s.Name.ToLower() == "font-size" || s.Name.ToLower() == "font-family").ToArray();
            if (cssMatches.Length > 0)
            {
                string style = string.Empty, variant = string.Empty, weight = string.Empty, size = string.Empty, family = string.Empty;
                for (int i = 0, l = cssMatches.Length; i < l; i++)
                {
                    //font-style font-variant font-weight font-size/line-height
                    if (cssMatches[i].Name.ToLower() == shortStyleName.ToLower())
                    {
                        string[] trbl = cssMatches[i].Setting.Split(' ');
                        style = trbl[0].Trim();
                        variant = (trbl.GetUpperBound(0) >= 1) ? trbl[1].Trim() : "";
                        weight = (trbl.GetUpperBound(0) >= 2) ? trbl[2].Trim() : "";
                        size = (trbl.GetUpperBound(0) >= 3) ? trbl[3].Trim() : "";
                        family = (trbl.GetUpperBound(0) >= 4) ? trbl[4].Trim() : "";
                    }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-style", shortStyleName))
                    { style = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-variant", shortStyleName))
                    { variant = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-weight", shortStyleName))
                    { weight = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-size", shortStyleName))
                    { size = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-family", shortStyleName))
                    { family = cssMatches[i].Setting.Trim(); }
                }

                if (!string.IsNullOrWhiteSpace(family) && !string.IsNullOrWhiteSpace(size))
                {
                    string strSetting = string.Format("{0} {1} {2} {3} {4}", style, variant, weight, size, family).Trim();
                    strSetting = Regex.Replace(strSetting, @"[ ]{2,}", @" ");

                    Style cssShortHand = new Style() { Name = shortStyleName, Setting = strSetting };

                    //if there is more than one or the shorthand version is shorter, then convert to short hand.
                    if (cssMatches.Length > 1 || string.Format("{0}{1}", cssShortHand.Name, cssShortHand.Setting).Length < string.Format("{0}{1}", lcsIncoming[0].Name, lcsIncoming[0].Setting).Length)
                    {
                        //Delete all current matches from collection
                        lcsIncoming.RemoveAll(css => css.Name.ToLower() == "font" || css.Name.ToLower() == "font-style" || css.Name.ToLower() == "font-variant" || css.Name.ToLower() == "font-weight" || css.Name.ToLower() == "font-size" || css.Name.ToLower() == "font-family");
                        //Add shorthand.
                        lcsIncoming.Add(cssShortHand);
                    }
                }
            }
        }

        /// <summary>
        /// Optimizes border styles
        /// </summary>
        /// <param name="lcsIncoming">style collection</param>
        /// <example>border:border-width border-style border-color</example>
        private void minimizeBorderStyles(StyleCollection lcsIncoming)
        {
            //Optimize
            string shortStyleName = "border";
            Style[] cssMatches = lcsIncoming.Where(s => s.Name.ToLower() == "border" || s.Name.ToLower() == "border-width" || s.Name.ToLower() == "border-style" || s.Name.ToLower() == "border-color").ToArray();
            if (cssMatches.Length > 0)
            {
                string width = string.Empty, style = string.Empty, color = string.Empty;
                for (int i = 0, l = cssMatches.Length; i < l; i++)
                {
                    //font-style font-variant font-weight font-size/line-height
                    if (cssMatches[i].Name.ToLower() == shortStyleName.ToLower())
                    {
                        string[] trbl = cssMatches[i].Setting.Split(' ');

                        if (trbl.GetUpperBound(0) == 0)
                        {
                            width = string.Empty;
                            style = trbl[0].Trim();
                            color = string.Empty;
                        }
                        else
                        {
                            width = trbl[0].Trim();
                            style = (trbl.GetUpperBound(0) >= 1) ? trbl[1].Trim() : "";
                            color = (trbl.GetUpperBound(0) >= 2) ? trbl[2].Trim() : "";
                        }
                    }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-width", shortStyleName))
                    { width = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-style", shortStyleName))
                    { style = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-color", shortStyleName))
                    { color = cssMatches[i].Setting.Trim(); }
                }

                if (!String.IsNullOrWhiteSpace(style))
                {
                    string strSetting = string.Format("{0} {1} {2}", width, style, color).Trim();
                    strSetting = Regex.Replace(strSetting, @"[ ]{2,}", @" ");

                    Style cssShortHand = new Style() { Name = shortStyleName, Setting = strSetting };

                    //if there is more than one or the shorthand version is shorter, then convert to short hand.
                    if (cssMatches.Length > 1 || string.Format("{0}{1}", cssShortHand.Name, cssShortHand.Setting).Length < string.Format("{0}{1}", lcsIncoming[0].Name, lcsIncoming[0].Setting).Length)
                    {
                        //Delete all current matches from collection
                        lcsIncoming.RemoveAll(css => css.Name.ToLower() == "border" || css.Name.ToLower() == "border-width" || css.Name.ToLower() == "border-style" || css.Name.ToLower() == "border-color");
                        //Add shorthand.
                        lcsIncoming.Add(cssShortHand);
                    }
                }
            }
        }

        /// <summary>
        /// Optimizes background styles
        /// </summary>
        /// <param name="lcsIncoming">style collection</param>
        /// <example>background:background-style background-variant background-weight background-size/line-height background-family</example>
        private void minimizeBackgroundStyles(StyleCollection lcsIncoming)
        {
            //Optimize
            string shortStyleName = "background";
            Style[] cssMatches = lcsIncoming.Where(s => s.Name.ToLower() == "background" || s.Name.ToLower() == "background-color" || s.Name.ToLower() == "background-image" || s.Name.ToLower() == "background-repeat" || s.Name.ToLower() == "background-attachment" || s.Name.ToLower() == "background-position").ToArray();
            if (cssMatches.Length > 0)
            {
                string color = string.Empty, image = string.Empty, repeat = string.Empty, attachment = string.Empty, position = string.Empty;
                for (int i = 0, l = cssMatches.Length; i < l; i++)
                {
                    //font-style font-variant font-weight font-size/line-height
                    if (cssMatches[i].Name.ToLower() == shortStyleName.ToLower())
                    {
                        string[] trbl = cssMatches[i].Setting.Split(' ');
                        color = trbl[0].Trim();
                        image = (trbl.GetUpperBound(0) >= 1) ? trbl[1].Trim() : "";
                        repeat = (trbl.GetUpperBound(0) >= 2) ? trbl[2].Trim() : "";
                        attachment = (trbl.GetUpperBound(0) >= 3) ? trbl[3].Trim() : "";
                        position = (trbl.GetUpperBound(0) >= 4) ? trbl[4].Trim() : "";
                    }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-color", shortStyleName))
                    { color = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-image", shortStyleName))
                    { image = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-repeat", shortStyleName))
                    { repeat = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-attachment", shortStyleName))
                    { attachment = cssMatches[i].Setting.Trim(); }
                    else if (cssMatches[i].Name.ToLower() == string.Format("{0}-position", shortStyleName))
                    { position = cssMatches[i].Setting.Trim(); }
                }

                string strSetting = string.Format("{0} {1} {2} {3} {4}", color, image, repeat, attachment, position).Trim();
                strSetting = Regex.Replace(strSetting, @"[ ]{2,}", @" ");

                Style cssShortHand = new Style() { Name = shortStyleName, Setting = strSetting };

                //if there is more than one or the shorthand version is shorter, then convert to short hand.
                if (cssMatches.Length > 1 || string.Format("{0}{1}", cssShortHand.Name, cssShortHand.Setting).Length < string.Format("{0}{1}", lcsIncoming[0].Name, lcsIncoming[0].Setting).Length)
                {
                    //Delete all current matches from collection
                    lcsIncoming.RemoveAll(css => css.Name.ToLower() == "background" || css.Name.ToLower() == "background-color" || css.Name.ToLower() == "background-image" || css.Name.ToLower() == "background-repeat" || css.Name.ToLower() == "background-attachment" || css.Name.ToLower() == "background-position");
                    //Add shorthand.
                    lcsIncoming.Add(cssShortHand);
                }
            }
        }

        /// <summary>
        /// Reorganizes selectors to group common styles.
        /// </summary>
        /// <param name="cscIncoming">Incoming CSC </param>
        private void reorganizeSelectors(CascadingStyleCollection cscIncoming)
        {
            CascadingStyleCollection reorganized = new CascadingStyleCollection();

            //List of css properties and values that are used multiple times
            var multiProp = cscIncoming.SelectMany(csc => csc.Styles.Select(s => new { name = s.Name, setting = s.Setting }))
                                    .GroupBy(bg => bg)
                                    .Where(w => w.Count() > 1)
                                    .Select(g => new { count = g.Count(), key = g.Key })
                                    .OrderBy(o => o.count)
                                    .ToList();

            for (int i = 0, l = multiProp.Count(); i < l; i++)
            {
                //Get list of Selectors to move
                var selectors = cscIncoming.Where((x, index) => x.Styles.Where(s => s.Name == multiProp[i].key.name && s.Setting == multiProp[i].key.setting).Count() > 0
                                && x.Name.Length < multiProp[i].key.name.Length // Only select styles that are longer than the class name
                                && index < ((cscIncoming.Select((csc, idx) => new { csc = csc, idx = idx })
                                            .Where(c => c.csc.Name == x.Name
                                                        && c.csc.Styles.Count(s => s.Name == multiProp[i].key.name && s.Setting != multiProp[i].key.setting) > 0)
                                                    .Select(q => (int?)q.idx).Min() == null) ? cscIncoming.Count() : cscIncoming.Select((csc, idx) => new { csc = csc, idx = idx })
                                            .Where(c => c.csc.Name == x.Name
                                                        && c.csc.Styles.Count(s => s.Name == multiProp[i].key.name && s.Setting != multiProp[i].key.setting) > 0)
                                                    .Select(q => (int?)q.idx).Min())
                            ).ToList();
                selectors.Count().ToString();

                for (int x = 0, c = selectors.Count(); x < c; x++)
                { 
                    //Remove styles from existing selector
                    cscIncoming.Where(r => r.Name == selectors[x].Name && r.Styles.Where(s => s.Name == multiProp[i].key.name && s.Setting == multiProp[i].key.setting).Count() > 0)
                        .ToList()
                        .ForEach(css => css.Styles.RemoveAll(s => s.Name == multiProp[i].key.name && s.Setting == multiProp[i].key.setting));

                    //Add selector to the begining of the csc collection.
                    CascadingStyle cs = new CascadingStyle();
                    cs.Name = selectors[x].Name;
                    cs.Styles.Add(new Style() { Name = multiProp[i].key.name, Setting = multiProp[i].key.setting });
                    reorganized.Insert(0, cs);
                    
                    //only remove the styles that are repeated not the entire class
                    //cscIncoming.Where(r => r.Name == selectors[x].Name  && r.Styles == selectors[x].Styles);
                }
            }

            //Order by selector count.
            var orderedSelectors = reorganized.SelectMany(csc => csc.Styles.Select(s => new {selector = csc.Name}))
                                    .GroupBy(bg => bg)
                                    .Select(g => new { count = g.Count(), key = g.Key })
                                    .OrderBy(x => x.count)
                                    .ToList();
            orderedSelectors.Count();

            //Loop through the selectors and flatten all other selectors that match the style
            CascadingStyleCollection csTemp = new CascadingStyleCollection();

            for (int i = 0, l = orderedSelectors.Count(); i < l; i++)
            { 
                //Get style for selector
                Style style = reorganized.First(r => r.Name == orderedSelectors[i].key.selector).Styles.First();

                var temp = reorganized.Where(w => w.Styles.Where(s => s == style).Count() > 0).Select(r => r.Name).ToArray();
                string tempSelecors = String.Join(",", reorganized.Where(w => w.Styles.Where(s => s.Name == style.Name && s.Setting == style.Setting).Count() > 0).Select(r => r.Name).OrderBy(o => o).ToArray());

                StyleCollection sc = new StyleCollection();
                sc.Add(style);
                csTemp.Add(new CascadingStyle() { Name = tempSelecors, Styles = sc });
            }

            //Flatten styles for each selector.
            var temp3 = csTemp.SelectMany(csc => csc.Styles.Select(s => new { selector = csc.Name}))
                                    .GroupBy(bg => bg)
                                    .Select(g => new { count = g.Count(), key = g.Key })
                                    .OrderBy(x => x.count)
                                    .ToList();

            for (int i = 0, l = temp3.Count(); i < l; i++)
            {
                var t = csTemp.Where(a => a.Name == temp3[i].key.selector).SelectMany(b=> b.Styles.Select(s => new { prop = s.Name, set = s.Setting })).Distinct().ToList();
                CascadingStyle cs = new CascadingStyle();
                cs.Name = temp3[i].key.selector;

                t.ForEach(a => 
                    cs.Styles.Add( new Style() {Name = a.prop, Setting = a.set})
                );
                cscIncoming.Insert(0, cs);
            }
        }
    }
}
