namespace GrpcGenerator.Generators.AdditionalActions;

public interface IAdditionalAction
{
    public void DoAdditionalAction(string projectRoot, string projectName);
}