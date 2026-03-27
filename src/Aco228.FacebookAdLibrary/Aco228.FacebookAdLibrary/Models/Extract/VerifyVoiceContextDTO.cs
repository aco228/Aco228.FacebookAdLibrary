namespace Aco228.FacebookAdLibrary.Models.Extract;

public class VerifyVoiceContextDTO
{
    public List<string> types { get; set; }
    public AdLibraryAllGeoFinServInfoDTO ad_library_all_geo_fin_serv_info { get; set; }
}


public class PayerInfoDTO
{
    public object license_info { get; set; }
    public List<object> license_info_list { get; set; }
    public string location { get; set; }
    public string name { get; set; }
    public string website { get; set; }
}

public class BeneficiaryInfoDTO
{
    public object license_info { get; set; }
    public List<object> license_info_list { get; set; }
    public string location { get; set; }
    public string name { get; set; }
    public string website { get; set; }
}

public class FinservDataDTO
{
    public string geo { get; set; }
    public bool is_payer_beneficiary_same { get; set; }
    public PayerInfoDTO payer_info { get; set; }
    public BeneficiaryInfoDTO beneficiary_info { get; set; }
}

public class AdLibraryAllGeoFinServInfoDTO
{
    public List<FinservDataDTO> finserv_data { get; set; }
}
