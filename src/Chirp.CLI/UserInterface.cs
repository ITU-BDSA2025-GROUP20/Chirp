public static class UserInterface
{
    public static void PrintCheeps(IEnumerable<Cheep> cheeps)
    {
        foreach (Cheep cheep in cheeps)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(Convert.ToDouble(cheep.Timestamp)).ToLocalTime();
            string[] switchedDayMonth = dateTime.ToString().Split('/');
            switchedDayMonth[2] = switchedDayMonth[2].TrimStart('2');
            switchedDayMonth[2] = switchedDayMonth[2].TrimStart('0');
            var time = switchedDayMonth[1] + "/" + switchedDayMonth[0] + "/" + switchedDayMonth[2];
            Console.WriteLine(cheep.Author + " @ " + time + ": " + cheep.Message);
        }
    }
}