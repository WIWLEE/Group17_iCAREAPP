﻿using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Group17_iCAREAPP.Models;

namespace Group17_iCAREAPP.Controllers
{
    [Authorize(Roles = "Doctor,Nurse")]
    public class PatientRecordController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        // GET: PatientRecords
        public ActionResult Index()
        {
            var patientRecords = db.PatientRecord
                .Include(p => p.iCAREWorker)
                .Include(p => p.GeoCodes)
                .ToList();
            return View(patientRecords);
        }

        // GET: PatientRecords/Create
        public ActionResult Create()
        {
            // Updated to use correct property names from GeoCodes model
            ViewBag.geographicalUnit = new SelectList(db.GeoCodes, "ID", "description");
            return View();
        }

        // POST: PatientRecords/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "name,address,dateOfBirth,height,weight,bloodGroup,bedID,treatmentArea,geographicalUnit")] PatientRecord patientRecord)
        {
            if (ModelState.IsValid)
            {
                patientRecord.ID = Guid.NewGuid().ToString();
                patientRecord.modifierID = User.Identity.Name;

                db.PatientRecord.Add(patientRecord);
                db.SaveChanges();

                TempData["Success"] = "Patient record created successfully.";
                return RedirectToAction("Index");
            }

            // Updated to use correct property names from GeoCodes model
            ViewBag.geographicalUnit = new SelectList(db.GeoCodes, "ID", "description", patientRecord.geographicalUnit);
            return View(patientRecord);
        }

        // GET: PatientRecords/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var patientRecord = db.PatientRecord.Find(id);
            if (patientRecord == null)
            {
                return HttpNotFound();
            }

            // Updated to use correct property names from GeoCodes model
            ViewBag.geographicalUnit = new SelectList(db.GeoCodes, "ID", "description", patientRecord.geographicalUnit);
            ViewBag.BloodGroups = new SelectList(
                new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" },
                patientRecord.bloodGroup
            );

            return View(patientRecord);
        }

        // POST: PatientRecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,name,address,dateOfBirth,height,weight,bloodGroup,bedID,treatmentArea,geographicalUnit")] PatientRecord patientRecord)
        {
            if (ModelState.IsValid)
            {
                patientRecord.modifierID = User.Identity.Name;
                db.Entry(patientRecord).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = "Patient record updated successfully.";
                return RedirectToAction("Index");
            }

            // Updated to use correct property names from GeoCodes model
            ViewBag.geographicalUnit = new SelectList(db.GeoCodes, "ID", "description", patientRecord.geographicalUnit);
            ViewBag.BloodGroups = new SelectList(
                new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" },
                patientRecord.bloodGroup
            );

            return View(patientRecord);
        }
    }
}