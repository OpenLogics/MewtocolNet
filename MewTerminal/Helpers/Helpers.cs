using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewTerminal;

internal static class Helpers {

    internal static Table ToTable<T> (this IEnumerable<T> data, params string[] markups) {

        // Create a table
        var table = new Table();
        var type = typeof(T);

        var props = type.GetProperties();
        bool isFirst = true;

        foreach (var item in data) {

            var rowVals = new List<string>();

            foreach (var prop in props) {

                if(isFirst) table.AddColumn(prop.Name);

                var propVal = prop.GetValue(item);

                string strVal = propVal?.ToString() ?? "null";

                if (propVal is byte[] bArr) {
                    strVal = string.Join(" ", bArr.Select(x => x.ToString("X2")));
                }

                strVal = strVal.Replace("[", "");
                strVal = strVal.Replace("]", "");

                rowVals.Add(strVal);

            }

            isFirst = false;

            table.AddRow(rowVals.ToArray());

        }

        return table;

    }

}
