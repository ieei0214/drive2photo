namespace drive2photo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var pathName = "";
            var drive2Photo = new Drive2Photo();

            // get the pathName from the command line
            Console.WriteLine("Enter the google drive path name: ");
            pathName = Console.ReadLine().Trim();

            if (pathName.Length>0)
            {
                await drive2Photo.Run(pathName);
            }
        }

    }
}
