namespace POS.Application;

public interface IReportExporter
{
    void Export(GroupReport report, string filePath);
}