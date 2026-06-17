using smartUI_fluent_Layout.examples;

namespace smartUI_fluent_Layout
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

			//var f1 = new Form1(); f1.Show();
			//var f2 = new Form2(); f2.Show();
			//new frm_TaskManager2().Show();
			//new frm_w11Settings().Show();
			//new frm_LayoutExample().Show();

			//Application.Run(new frm_w11Settings());

			Application.Run(new frm_TaskManager2());

			//Application.Run(new frm_Components_example());


			

		}
    }
}