using CsvHelper.Configuration;
using InfotecsTest.Domain;
using System.Globalization;

namespace InfotecsTest.Models;

public class ValueDTO: ClassMap<ValueData>
{
    public ValueDTO()
    {
        Map(m => m.Date)
            .Name("Date")
            .TypeConverterOption.Format("yyyy-MM-ddTHH-mm-ss.fffZ")
            .TypeConverterOption.DateTimeStyles(
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal
            );
        Map(m => m.ExecutionTime).Name("ExecutionTime");
        Map(m => m.Value).Name("Value");
    }
}
