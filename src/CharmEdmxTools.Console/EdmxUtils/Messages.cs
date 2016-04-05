using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CharmEdmxTools.EdmxUtils
{
    public class Messages
    {
        static Messages()
        {
            SetCurrent(Thread.CurrentThread.CurrentCulture.LCID.ToString());
        }

        public static void SetCurrent(string local)
        {
            if (local.Equals("it", StringComparison.OrdinalIgnoreCase) || local == "1040")
                Current = new MessagesIt();
            else
                Current = new Messages();
        }
        public static Messages Current { get; private set; }

        public virtual string Avvioelaborazionedi { get { return "Start processing '{0}'"; } }
        public virtual string SavingEdmx { get { return "Saving EDMX..."; } }
        public virtual string RielaborazioneEdmx { get { return "Reworking EDMX with custom tools T4"; } }
        public virtual string OperazioneTerminataConSuccesso { get { return "The operation completed successfully."; } }
        public virtual string OperazioneTerminataSenzaModifiche { get { return "Operation terminated. There was no modification."; } }
        public virtual string AggiuntoFileASourceControl { get { return "Added file '{0}' to Source Control."; } }
        public virtual string EseguitoFixPropertiesAttributes { get { return "Executed FixPropertiesAttributes on '{0}.{1}' - {2}"; } }
        public virtual string ErroreFixPropertiesAttributes { get { return "ERROR: FixPropertiesAttributes - Type {0} not managed"; } }
        public virtual string AvvioClearEdmxPreservingKeyFields { get { return "Starting ClearEdmxPreservingKeyFields: all non-key fields will be deleted."; } }
        public virtual string EliminazioneEntityDaConceptualModels { get { return "Elimination of entity '{0}' to Conceptual Models (Entity not found in Storage Models)."; } }
        public virtual string EliminazionePropertyDaConceptualModels { get { return "Elimination of the property '{0}. {1}' to Conceptual Models (Property not found in the pattern of StorageModels)."; } }
        public virtual string ErroreImpossibileEliminarePropertyDaStorage { get { return "ERROR: Can not delete the property '{0}. {1}' by Storage Models, it is a Key (Property not found in the pattern of ConceptualModels)."; } }
        public virtual string EliminazionePropertyDaStorageModels { get { return "Elimination of the property '{0}. {1}' by Storage Models (Property not found in the pattern of ConceptualModels)."; } }
        public virtual string EliminazioneMappingsEntityDaMappings { get { return "Deleting Mappings for Entity: '{0}', Property: '{1}', Column Name: '{2}' from Mappings (Property not found in the pattern of ConceptualModels and StorageModels)"; } }
        public virtual string RinominoNavigationProperty { get { return "NavigationProperty rename in '{0}' from '{1}' to '{2}'"; } }
        public virtual string CambioValoreMultiplicityFk { get { return "Change Multiplicity value of FK: '{0}' for Role: '{1}' to '{2}' to '{3}'"; } }

        public virtual string CreatedConfig { get { return "Created config in '{0}'. Please, check it and re-execute command."; } }
    }

    public class MessagesIt : Messages
    {
        public override string Avvioelaborazionedi { get { return "Avvio elaborazione di '{0}'"; } }
        public override string SavingEdmx { get { return "Salvataggio EDMX..."; } }
        public override string RielaborazioneEdmx { get { return "Rielaborazione EDMX con strumenti personalizzati T4"; } }
        public override string OperazioneTerminataConSuccesso { get { return "Operazione terminata con successo."; } }
        public override string OperazioneTerminataSenzaModifiche { get { return "Operazione terminata. Non è stata apportata nessuna modifica."; } }
        public override string AggiuntoFileASourceControl { get { return "Aggiunto file '{0}' a SourceControl."; } }
        public override string EseguitoFixPropertiesAttributes { get { return "Eseguito FixPropertiesAttributes su '{0}.{1}' - {2}"; } }
        public override string ErroreFixPropertiesAttributes { get { return "ERRORE: FixPropertiesAttributes - Tipo {0} non gestito"; } }
        public override string AvvioClearEdmxPreservingKeyFields { get { return "Avvio di ClearEdmxPreservingKeyFields: tutti i campi non key verranno eliminati."; } }
        public override string EliminazioneEntityDaConceptualModels { get { return "Eliminazione dell'entity '{0}' da ConceptualModels (Entity non trovata in StorageModels)"; } }
        public override string EliminazionePropertyDaConceptualModels { get { return "Eliminazione della property '{0}.{1}' da ConceptualModels (Property non trovata nel modello di StorageModels)"; } }
        public override string ErroreImpossibileEliminarePropertyDaStorage { get { return "ERROR: Impossibile eliminazione la property '{0}.{1}' da StorageModels, è una Key (Property non trovata nel modello di ConceptualModels)"; } }
        public override string EliminazionePropertyDaStorageModels { get { return "Eliminazione della property '{0}.{1}' da StorageModels (Property non trovata nel modello di ConceptualModels)"; } }
        public override string EliminazioneMappingsEntityDaMappings { get { return "Eliminazione Mappings per Entity: '{0}', Propery: '{1}', ColumnName: '{2}' da Mappings (Property non trovata nel modello di ConceptualModels e di StorageModels)"; } }
        public override string RinominoNavigationProperty { get { return "Rinomino NavigationProperty in '{0}' da '{1}' a '{2}'"; } }
        public override string CambioValoreMultiplicityFk { get { return "Cambio valore Multiplicity su FK: '{0}' per Role: '{1}' da '{2}' a '{3}'"; } }
    }

}
