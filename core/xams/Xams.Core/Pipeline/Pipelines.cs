using Xams.Core.Attributes;
using Xams.Core.Pipeline.Stages;

namespace Xams.Core.Pipeline;

public static class Pipelines
{
    public static readonly PipelineBuilder SecurityPipeline = new PipelineBuilder()
        .Add(new PipeSetDefaultFields())
        .Add(new PipePermissions());
    
    public static readonly PipelineBuilder Create = new PipelineBuilder()
        .Add(new PipePreValidation())
        .Add(new PipeProtectSystemRecords())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreValidation))
        .Add(new PipeValidateNonNullableProperties())
        .Add(new PipeAddEntityToEntities())
        .Add(new PipePermissionRules())
        .Add(new PipeUIServices())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreOperation))
        .Add(new PipeEntityCreate())
        .Add(new PipeExecuteServiceLogic(LogicStage.PostOperation))
        .Add(new PipeResultEntity());
    
    public static readonly PipelineBuilder Update = new PipelineBuilder()
        .Add(new PipePreValidation())
        .Add(new PipeProtectSystemRecords())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreValidation))
        .Add(new PipePatchEntity())
        .Add(new PipeValidateNonNullableProperties())
        .Add(new PipeAddEntityToEntities())
        .Add(new PipePermissionRules())
        .Add(new PipeUIServices())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreOperation))
        .Add(new PipeEntityUpdate())
        .Add(new PipeExecuteServiceLogic(LogicStage.PostOperation))
        .Add(new PipeResultEntity());

    public static readonly PipelineBuilder Delete = new PipelineBuilder()
        .Add(new PipePreValidation())
        .Add(new PipeProtectSystemRecords())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreValidation))
        .Add(new PipeAddEntityToEntities())
        .Add(new PipePermissionRules())
        .Add(new PipeUIServices())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreOperation))
        .Add(new PipeEntityDelete())
        .Add(new PipeExecuteServiceLogic(LogicStage.PostOperation))
        .Add(new PipeResultEmpty());
    
    public static readonly PipelineBuilder Read = new PipelineBuilder()
        .Add(new PipePreValidation())
        .Add(new PipePermissions())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreValidation))
        .Add(new PipeExecuteServiceLogic(LogicStage.PreOperation))
        .Add(new PipeEntityRead())
        .Add(new PipeExecuteServiceLogic(LogicStage.PostOperation))
        .Add(new PipeResultReadOutput());
    
    // Proxy calls don't execute against the database or retrieve the PreEntity
    public static readonly PipelineBuilder CreateProxy = new PipelineBuilder()
        .Add(new PipePreValidation())
        // .Add(new PipeSetDefaultFields())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreValidation))
        .Add(new PipeValidateNonNullableProperties())
        .Add(new PipeAddEntityToEntities())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreOperation))
        .Add(new PipeExecuteServiceLogic(LogicStage.PostOperation))
        .Add(new PipeResultEntity());
    
    // Proxy calls don't execute against the database or retrieve the PreEntity
    public static readonly PipelineBuilder UpdateProxy = new PipelineBuilder()
        .Add(new PipePreValidation())
        // .Add(new PipeSetDefaultFields())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreValidation))
        .Add(new PipeValidateNonNullableProperties())
        .Add(new PipeAddEntityToEntities())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreOperation))
        .Add(new PipeExecuteServiceLogic(LogicStage.PostOperation))
        .Add(new PipeResultEntity());
    
    // Proxy calls don't execute against the database or retrieve the PreEntity
    public static readonly PipelineBuilder DeleteProxy = new PipelineBuilder()
        .Add(new PipePreValidation())
        .Add(new PipeAddEntityToEntities())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreValidation))
        .Add(new PipeExecuteServiceLogic(LogicStage.PreOperation))
        .Add(new PipeExecuteServiceLogic(LogicStage.PostOperation))
        .Add(new PipeResultEmpty());
    
    // Proxy calls don't execute against the database or retrieve the PreEntity
    public static readonly PipelineBuilder ReadProxy = new PipelineBuilder()
        .Add(new PipePreValidation())
        .Add(new PipeExecuteServiceLogic(LogicStage.PreValidation))
        .Add(new PipeExecuteServiceLogic(LogicStage.PreOperation))
        .Add(new PipeExecuteServiceLogic(LogicStage.PostOperation))
        .Add(new PipeResultReadOutput());
}