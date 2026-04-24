namespace PCcontroller
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            using var mutex = new Mutex(true, "PCController_SingleInstance", out bool isNew);
            if (!isNew)
            {
                MessageBox.Show("PCController zaten çalışıyor.");
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Main());
        }
    }
}