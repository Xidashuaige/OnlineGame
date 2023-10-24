public class User : NetWorkMessage
{
    public uint Id {  get; set; }
    public string Name { get; set; }

    private uint _id;

    private string _name;
}
