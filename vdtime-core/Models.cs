public class DesktopAndTime
{
  public required DesktopInfo Desktop { get; set; }
  public required TimeInfo Time { get; set; }

  public override string ToString() => $"{Desktop.Name}({Desktop.Id}) = {Time.Total}s";
}

public class TimeInfo
{
  public required UInt64 Current { get; set; }
  public required UInt64 Total { get; set; }
}

public class DesktopInfo
{
  public required string Name { get; set; }

  public required Guid Id { get; set; }
}
