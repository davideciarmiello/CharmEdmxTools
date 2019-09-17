namespace CharmEdmxTools.Core.CoreGlobalization
{
    public class MessagesEn : IMessages
    {
        public virtual string Avvioelaborazionedi { get { return "Start processing '{0}'"; } }
        public virtual string SavingEdmx { get { return "Saving EDMX..."; } }
        public virtual string SavingEdmxAfterFixIn { get { return "Saving EDMX after applied fixs in {0}"; } }
        public virtual string SavedEdmxIn { get { return "Saved EDMX in {0}"; } }
        public virtual string RielaborazioneEdmx { get { return "Reworking EDMX with custom tools T4"; } }
        public virtual string OperazioneTerminataConSuccesso { get { return "The operation completed successfully."; } }
        public virtual string OperazioneTerminataConSuccessoIn { get { return "The operation completed successfully in {0}."; } }
        public virtual string OperazioneTerminataSenzaModifiche { get { return "Operation terminated. There was no modification."; } }
        public virtual string OperazioneTerminataSenzaModificheIn { get { return "Operation terminated. There was no modification in {0}."; } }
        public virtual string AvvioVerificaFilesSourceControl { get { return "Starting check of added files at Source Control."; } }
        public virtual string AggiuntoFileASourceControl { get { return "Added file '{0}' to Source Control."; } }
        public virtual string EdmMappingConfigurationNotFound { get { return "EdmMappingConfiguration not found for provider '{0}'."; } }
        public virtual string EseguitoFixPropertiesAttributes { get { return "Executed FixPropertiesAttributes on '{0}.{1}' - {2}"; } }
        public virtual string ErroreFixPropertiesAttributes { get { return "ERROR: FixPropertiesAttributes - Type {0} not managed"; } }
        public virtual string AvvioClearEdmxPreservingKeyFields { get { return "Starting ClearEdmxPreservingKeyFields: all non-key fields will be deleted."; } }
        public virtual string EliminazioneEntityDaConceptualModels { get { return "Elimination of entity '{0}' to Conceptual Models (Entity not found in Storage Models)."; } }
        public virtual string EliminazioneEntityDaStorageModels { get { return "Elimination of entity '{0}' to Storage Models (Entity not found in Conceptual Models)."; } }
        public virtual string EliminazioneEntityDaStorageModelsQuestion { get { return "Attention: The following tables are not present in ConceptualModels. Delete them from StorageModels?\r\n{0}"; } }
        public virtual string EliminazioneAssociationDaConceptualModels { get { return "Elimination of the association '{0}' ({1}.{2} -> {3}.{4}) to Conceptual Models (Association not found in the pattern of StorageModels)."; } }
        public virtual string EliminazionePropertyDaConceptualModels { get { return "Elimination of the property '{0}. {1}' to Conceptual Models (Property not found in the pattern of StorageModels)."; } }
        public virtual string ErroreImpossibileEliminarePropertyDaStorage { get { return "ERROR: Can not delete the property '{0}. {1}' by Storage Models, it is a Key (Property not found in the pattern of ConceptualModels)."; } }
        public virtual string EliminazionePropertyDaStorageModels { get { return "Elimination of the property '{0}. {1}' by Storage Models (Property not found in the pattern of ConceptualModels)."; } }
        public virtual string EliminazioneMappingsEntityDaMappings { get { return "Deleting Mappings for Entity: '{0}', Property: '{1}', Column Name: '{2}' from Mappings (Property not found in the pattern of ConceptualModels and StorageModels)"; } }
        public virtual string RinominoNavigationProperty { get { return "NavigationProperty rename in '{0}' from '{1}' to '{2}'"; } }
        public virtual string CambioValoreMultiplicityFk { get { return "Change Multiplicity value of FK: '{0}' for Role: '{1}' to '{2}' to '{3}'"; } }

        public virtual string CreatedConfig { get { return "Created config in '{0}'. Please, check it and re-execute command."; } }
    }

}
