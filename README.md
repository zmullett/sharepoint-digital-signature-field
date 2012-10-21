sharepoint-digital-signature-field
==================================

Digital Signature Field for SharePoint Lists

There are many instances in business procedures where a mark of authority is required for approval or recognition. For example, the approval of a purchase order, or the 'signing off' of a completed procedure. A digital signature is the modern-day equivalent of the penned signature on paper. 

The Digital Signature Field is a means of adding digital signature capability to SharePoint lists. Users need simply to enter their password to sign items. Password entry at time of signing is the safest means of attaching a digital signature. The resulting signature is an encrypted message using a one-way security algorithm. This algorithm prevents fraudulent creation or transfer of signatures between lists, items and item versions. 

The Digital Signature Field can be added to any SharePoint list. Simply add a new column to your list of type 'Digital Signature'. You can add as many signature fields as you need to a list. For example, you might have signatures for different steps in a process.


Notes:

* The Digital Signature Field is designed for use with SharePoint lists. It will work with document libraries, but there are some small caveats.

* The Digital Signature Field does not fulfull the same function as SharePoint's internal content approval system. The content approval system handles management and public release of draft/final item revisions. The Digital Signature Field is a means of reliably marking an item with a user's authority.