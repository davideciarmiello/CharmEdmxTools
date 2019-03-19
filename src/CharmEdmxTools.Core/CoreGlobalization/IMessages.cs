using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharmEdmxTools.Core.CoreGlobalization
{
    public interface IMessages
    {
        string Avvioelaborazionedi { get; }
        string SavingEdmx { get; }
        string SavingEdmxAfterFixIn { get; }
        string SavedEdmxIn { get; }
        string RielaborazioneEdmx { get; }
        string OperazioneTerminataConSuccesso { get; }
        string OperazioneTerminataConSuccessoIn { get; }
        string OperazioneTerminataSenzaModifiche { get; }
        string OperazioneTerminataSenzaModificheIn { get; }
        string AvvioVerificaFilesSourceControl { get; }
        string AggiuntoFileASourceControl { get; }
        string EdmMappingConfigurationNotFound { get; }
        string EseguitoFixPropertiesAttributes { get; }
        string ErroreFixPropertiesAttributes { get; }
        string AvvioClearEdmxPreservingKeyFields { get; }
        string EliminazioneEntityDaConceptualModels { get; }
        string EliminazioneAssociationDaConceptualModels { get; }
        string EliminazionePropertyDaConceptualModels { get; }
        string ErroreImpossibileEliminarePropertyDaStorage { get; }
        string EliminazionePropertyDaStorageModels { get; }
        string EliminazioneMappingsEntityDaMappings { get; }
        string RinominoNavigationProperty { get; }
        string CambioValoreMultiplicityFk { get; }
        string CreatedConfig { get; }
    }
}
