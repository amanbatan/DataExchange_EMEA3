
namespace DataExchangeSrv
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Variabile di progettazione necessaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Pulire le risorse in uso.
        /// </summary>
        /// <param name="disposing">ha valore true se le risorse gestite devono essere eliminate, false in caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Codice generato da Progettazione componenti

        /// <summary>
        /// Metodo necessario per il supporto della finestra di progettazione. Non modificare
        /// il contenuto del metodo con l'editor di codice.
        /// </summary>
        private void InitializeComponent()
        {
            this.ServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.DataExchangeSrvInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // ServiceProcessInstaller
            // 
            this.ServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ServiceProcessInstaller.Password = null;
            this.ServiceProcessInstaller.Username = null;
            this.ServiceProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.ServiceProcessInstaller_AfterInstall);
            // 
            // DataExchangeSrvInstaller
            // 
            this.DataExchangeSrvInstaller.DisplayName = "DataExchangeSRV";
            this.DataExchangeSrvInstaller.ServiceName = "DataExchangeSRV";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ServiceProcessInstaller,
            this.DataExchangeSrvInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller DataExchangeSrvInstaller;
    }
}