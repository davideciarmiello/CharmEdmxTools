namespace CharmEdmxTools.Core.CoreGlobalization
{
    public class MessagesIt : IMessages
    {
        public string Avvioelaborazionedi { get { return "Avvio elaborazione di '{0}'"; } }
        public string SavingEdmx { get { return "Salvataggio EDMX..."; } }
        public string SavingEdmxAfterFixIn { get { return "Salvataggio EDMX dopo aver applicato modifiche in {0}..."; } }
        public string SavedEdmxIn { get { return "EDMX salvato in {0}..."; } }
        public string RielaborazioneEdmx { get { return "Rielaborazione EDMX con strumenti personalizzati T4"; } }
        public string OperazioneTerminataConSuccesso { get { return "Operazione terminata con successo."; } }
        public string OperazioneTerminataConSuccessoIn { get { return "Operazione terminata con successo in {0}."; } }
        public string OperazioneTerminataSenzaModifiche { get { return "Operazione terminata. Non è stata apportata nessuna modifica."; } }
        public string OperazioneTerminataSenzaModificheIn { get { return "Operazione terminata. Non è stata apportata nessuna modifica in {0}."; } }
        public string AvvioVerificaFilesSourceControl { get { return "Avvio verifica dei files aggiunti al Source Control."; } }
        public string AggiuntoFileASourceControl { get { return "Aggiunto file '{0}' a SourceControl."; } }
        public string EseguitoFixPropertiesAttributes { get { return "Eseguito FixPropertiesAttributes su '{0}.{1}' - {2}"; } }
        public string ErroreFixPropertiesAttributes { get { return "ERRORE: FixPropertiesAttributes - Tipo {0} non gestito"; } }
        public string AvvioClearEdmxPreservingKeyFields { get { return "Avvio di ClearEdmxPreservingKeyFields: tutti i campi non key verranno eliminati."; } }
        public string EliminazioneEntityDaConceptualModels { get { return "Eliminazione dell'entity '{0}' da ConceptualModels (Entity non trovata in StorageModels)"; } }
        public string EliminazioneAssociationDaConceptualModels { get { return "Eliminazione dell'associazione '{0}' ({1}.{2} -> {3}.{4}) da ConceptualModels (Association non trovata in StorageModels)."; } }
        public string EliminazionePropertyDaConceptualModels { get { return "Eliminazione della property '{0}.{1}' da ConceptualModels (Property non trovata nel modello di StorageModels)"; } }
        public string ErroreImpossibileEliminarePropertyDaStorage { get { return "ERROR: Impossibile eliminazione la property '{0}.{1}' da StorageModels, è una Key (Property non trovata nel modello di ConceptualModels)"; } }
        public string EliminazionePropertyDaStorageModels { get { return "Eliminazione della property '{0}.{1}' da StorageModels (Property non trovata nel modello di ConceptualModels)"; } }
        public string EliminazioneMappingsEntityDaMappings { get { return "Eliminazione Mappings per Entity: '{0}', Propery: '{1}', ColumnName: '{2}' da Mappings (Property non trovata nel modello di ConceptualModels e di StorageModels)"; } }
        public string RinominoNavigationProperty { get { return "Rinomino NavigationProperty in '{0}' da '{1}' a '{2}'"; } }
        public string CambioValoreMultiplicityFk { get { return "Cambio valore Multiplicity su FK: '{0}' per Role: '{1}' da '{2}' a '{3}'"; } }
        public string CreatedConfig { get { return "File di config salvato in '{0}'. Verificarlo e rieseguire il comando."; } }
    }
}