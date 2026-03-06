namespace SmartWaste.Web.Models
{
    public class SqlTableResult
    {
        public string Title { get; set; } = "";
        public List<string> Columns { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
    }
}
