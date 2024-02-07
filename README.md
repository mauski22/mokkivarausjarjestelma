# mokkivarausjarjestelma
A school group-project. I created FormMokitJaMokkivaraukset.cs and FormVaraus.cs

The forms have following functionality:
The renting company can add cabins into the database. A cabin has it's ID, cabin description, name, street addresse, cabin size, ID of the area where it is located, price per day, cottage features and equipment.
The cabins can be searched from the database by their post numbers and area ID's. 
The company can arrange reservations on the individual cabins, which are put into the database. The reservations can also be edited or removed. What is stored: the reservation ID, buyer's name, cabin ID, start date, end date.

* Problems like crashes, overlapping dates, deleting cabins that have reservations made to them etc, are addressed.

This is how the app works:
A Cabin renting company...
- First adds 'areas' (for example Tahko or Pyhätunturi) to the database from the app. Also the city and post number can be added. Done on the 'Area' page.
- Then adds the cabins they want to rent, into the database. <=== (What I did, the cabin page)
- Can also add different services (if they provide them. Reindeer rides, for example) into the database. Done on the Services page.

When those steps are done, customers can call the company to make a reservation of a certain cabin.
And the company...
- Collects the customer's information, which is put into the database on customer-page.
- Makes the cabin reservation with the customers information and the cabin information, on the Cabin Reservation page. <=== (What I did)
- Can let customer choose from the services available in that area
- Saves the customer's order into a bill, that goes into the database. That is done in the Billing page. The company can choose to get the bill as a PDF-file, to send to the customer.
