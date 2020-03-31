/******
* Project   : IFMISReportViewer
* Author    : Kajal Malde
* Purpose   : Company Maintainence Controller
* Version   : 2.0.0.0
******/
//Version     Modified By           Modified On   Purpose
//========   ====================  ===========   ========================================================================================================
//2.0.0.1    Kajal Malde            28-06-2019   23567	All insertions should be by default active for ease .23565	Export Excel of all matrix should have same terminology as GUI
//2.0.0.2    Kajal Malde            22-10-2019   24889	On clicking Company Maintenance link showing Server Error
//2.0.0.3    Kajal Malde            28-11-2019   24906  Validation message is not showing when importing existing company which is already present on the Company Maintenace list
//2.0.0.4    Kajal Malde            24-12-2019   24951	Company Maintenance : Restrict assigned active company's status changing to 'In-Active',26977	List of bugs & enhancement while Demo smoke test on QA environment
//                                               27362	Usergroup & Company : incorrect message showing when deleting a assigned Usergroup


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using IRPV2DBModel;
using System.Data.Entity;
using System.Data;
using IFMISReportVersion2.Models;

namespace IFMISReportVersion2.Controllers
{
    public class CompanyController : Controller
    {
        StclIRPV2Entities db = new StclIRPV2Entities();
        ActivityLogVM alm = new ActivityLogVM();
        // GET: Company/View Details Of Company

        public ActionResult Index()
        {
            var login = (User)Session["Login"];
            TempData["Compvalue"] = Session["Compvalue"];
            TempData["CompName"] = Session["CompName"];
            if (login != null)
            {
                try
                {
                    TempData["IsAdd"] = string.Empty;
                    if (Utility.Instance.IsHasRights(login.UserID, "Company Maintenance", "add") == true)
                    {
                        TempData["IsAdd"] = "IsAdd";
                    }
                    var complist = db.Companies.ToList();
                    return View(complist);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message.ToString();
                    return View();
                }
                finally
                {
                    if (TempData["ErrorMessage"] != null)
                    {
                        alm.AddActivityLog(login.UserID, login.UserName, TempData["ErrorMessage"].ToString());
                    }
                }
            }
            else
            {
                return RedirectToAction("AuthError", "Home");
            }
        }
        // GET: Add new Company/Update Existing Company
        public ActionResult AddOrEdit(int id = 0)
        {
            var login = (User)Session["Login"];
            TempData["Compvalue"] = Session["Compvalue"];
            TempData["CompName"] = Session["CompName"];
            if (login != null)
            {
                try
                {
                    if (id == 0)
                    {
                        CompanyVM c = new Company();
                        return View(c);
                    }
                    else
                    {
                        CompanyVM c = db.Companies.Find(id);
                        return View(c);
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message.ToString();
                    return View(new CompanyVM());
                }
                finally
                {
                    if (TempData["ErrorMessage"] != null)
                    {
                        alm.AddActivityLog(login.UserID, login.UserName, TempData["ErrorMessage"].ToString());
                    }
                }

            }
            else
            {
                return RedirectToAction("AuthError", "Home");
            }
        }

        // POST: Save new Company/Update Existing Company
        [HttpPost]

        public ActionResult AddOrEdit(CompanyVM cmp)
        {
            var login = (User)Session["Login"];
            TempData["Compvalue"] = Session["Compvalue"];
            TempData["CompName"] = Session["CompName"];
            if (login != null)
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        if (Utility.Instance.IsCompExist(cmp.CompName, cmp.CompID) == true)
                        {
                            Company c = new Company();
                            c = cmp;
                            if (cmp.CompID == 0)
                            {
                                c.CompStatus = true;
                                c.CompInsBy = login.UserID;
                                c.CompInsOn = DateTime.Now;
                                db.Companies.Add(c);
                                db.SaveChanges();
                                TempData["SuccessMessage"] = cmp.CompName + " Saved Successfully";
                            }
                            else
                            {
                                var IsCompsAssociated = db.UserCompanyAssociations.Where(q => q.CompID == c.CompID).ToList();
                                if (IsCompsAssociated.Count > 0 && c.CompStatus== false)
                                {
                                    TempData["ErrorMessage"] = "This company is associated with user so cannot be In active.";
                                    return View(cmp);
                                }
                                c.CompUpBy = login.UserID;
                                c.CompUpOn = DateTime.Now;
                                db.Entry(c).State = EntityState.Modified;
                                db.SaveChanges();
                                TempData["SuccessMessage"] = cmp.CompName + " Updated Successfully";
                            }
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Duplicate Company Name:" + cmp.CompName + "Found.";
                            return RedirectToAction("Index");
                        }

                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = ex.Message.ToString();
                        return View(cmp);
                    }
                    finally
                    {
                        if (TempData["ErrorMessage"] != null)
                        {
                            alm.AddActivityLog(login.UserID, login.UserName, TempData["ErrorMessage"].ToString());
                        }
                        else
                        {
                            alm.AddActivityLog(login.UserID, login.UserName, TempData["SuccessMessage"].ToString());
                        }
                    }

                }
                else
                {
                    TempData["ErrorMessage"] = "Please enter data in mandatory fields.";
                    return View(cmp);
                }
            }
            else
            {
                return RedirectToAction("AuthError", "Home");
            }
        }

        // POST: Delete selected Company
        public ActionResult Delete(int id)
        {
            var login = (User)Session["Login"];
            TempData["Compvalue"] = Session["Compvalue"];
            TempData["CompName"] = Session["CompName"];
            if (login != null)
            {
                try
                {
                    Company company = db.Companies.Find(id);
                    var IsCompsAssociated = db.UserCompanyAssociations.Where(q => q.CompID == id).ToList();
                    if (IsCompsAssociated.Count > 0 )
                    {
                        TempData["ErrorMessage"] = "This company is associated with user so cannot delete.";
                        return RedirectToAction("Index");
                    }
                    db.Companies.Remove(company);
                    db.SaveChanges();
                 
                    TempData["SuccessMessage"] = "Deleted Successfully";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message.ToString();
                    return RedirectToAction("Index");
                }
                finally
                {
                    if (TempData["ErrorMessage"] != null)
                    {
                        alm.AddActivityLog(login.UserID, login.UserName, TempData["ErrorMessage"].ToString());
                    }
                    else if (TempData["SuccessMessage"] != null)
                    {
                        alm.AddActivityLog(login.UserID, login.UserName, TempData["SuccessMessage"].ToString());
                    }
                }

            }
            else
            {
                return RedirectToAction("AuthError", "Home");
            }
        }
        GetCompaniesFromEpicor g = new GetCompaniesFromEpicor();
        [OverrideActionFilters]
        // GET: Delete selected Company
        public ActionResult GetCompaniesFromEpicor()
        {
            var login = (User)Session["Login"];
            TempData["Compvalue"] = Session["Compvalue"];
            TempData["CompName"] = Session["CompName"];
            if (login != null)
            {
                if (Utility.Instance.IsHasRights(login.UserID, "Company Maintenance", "add") == true)
                {
                    try
                    {
                        g.Companies = g.PopulateCompanies();
                        return View(g);
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = ex.Message.ToString();
                        return RedirectToAction("Index");
                    }
                    finally
                    {
                        if (TempData["ErrorMessage"] != null)
                        {
                            alm.AddActivityLog(login.UserID, login.UserName, TempData["ErrorMessage"].ToString());
                        }

                    }
                }
                else
                {
                    return RedirectToAction("NoAccess", "Home");
                }
            }
            else
            {
                return RedirectToAction("AuthError", "Home");
            }

        }
        [HttpPost]
        [OverrideActionFilters]
        public ActionResult GetCompaniesFromEpicor(GetCompaniesFromEpicor getcom)
        {
            var login = (User)Session["Login"];
            TempData["Compvalue"] = Session["Compvalue"];
            TempData["CompName"] = Session["CompName"];
            if (login != null)
            {
                try
                {
                    getcom.Companies = g.PopulateCompanies();
                    if (getcom.SelectedCompanies != null)
                    {
                        List<SelectListItem> selectedItems = getcom.Companies.Where(p => getcom.SelectedCompanies.Contains(p.Value)).ToList();
                        ViewBag.Message = "Selected Companies:";
                        foreach (var selectedItem in selectedItems)
                        {
                            Company cmp = new Company();
                            cmp.CompCode = selectedItem.Value;
                            cmp.CompName = selectedItem.Text;
                            cmp.CompDisplayName = selectedItem.Text;
                            cmp.CompStatus = true;
                            if (cmp.CompCode == string.Empty && cmp.CompName == "No Value")
                            {
                                TempData["ErrorMessage"] = "Incorrect Company Connection String in Config File.Contact System Administrator.";
                            }
                            else
                            {
                                if (Utility.Instance.IsCompExist(cmp.CompName, cmp.CompID) == true)
                                {
                                    db.Companies.Add(cmp);
                                    db.SaveChanges();
                                    alm.AddActivityLog(login.UserID, login.UserName, "New Company Created: " + cmp.CompName);
                                    TempData["SuccessMessage"] = "Company imported successfully.";
                                }
                                else
                                {
                                    TempData["ErrorMessage"] = "Already Exists. ";
                                }
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message.ToString();
                }
                finally
                {
                    if (TempData["ErrorMessage"] != null)
                    {
                        alm.AddActivityLog(login.UserID, login.UserName, TempData["ErrorMessage"].ToString());
                    }
                }

                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("AuthError", "Home");
            }
        }

        //ExportToExcel Company List
        public ActionResult ExportToExcel(FormCollection fc)
        {
            var login = (User)Session["Login"];
            TempData["Compvalue"] = Session["Compvalue"];
            TempData["CompName"] = Session["CompName"];
            if (login != null)
            {
                DataTable dt = Utility.Instance.ToDataTable(db.Companies.ToList());
                Utility.Instance.ExporttoExcel(dt, "Company List _" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                alm.AddActivityLog(login.UserID, login.UserName, "Exported Data to Excel File: " + "Company List _" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("AuthError", "Home");
            }
        }

    }
}