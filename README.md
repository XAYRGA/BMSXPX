# BMSXPX - JAudio Engine Emulator / BMS Player 

Please note that you'll need my fork of SharpDX to use this until my PR is merged, you can find it here.  https://github.com/TheFreezebug/SharpDX . Alternatively, you can use the contents found in the "prebuilt" folder if you don't want to use it. 

Note that this CANNOT play back all sequences, this is a work in progress, that i don't know if it will ever be completed. 

The code is a mess, and difficult to read. 



Special Thanks / Credits: 


Jasper - https://github.com/magcius/  (Lots of personal help with this! Thank you so much!)

Yoshimaster96 - https://github.com/Yoshimaster96/BMS_DEC/ (Code reference)

RenolY2 - https://github.com/RenolY2/py-playBMS  (Code reference)
 
Arookas - https://github.com/arookas (BMS Discoveries / Code reference)


# Hey wait but how do i: 

It's fairly straight forward, i'd include an example, but due to copyright laws, that's forbidden. 

Basically, the initialization file for the game must be dropped in the same directory as BMSXPX, and named "JaiInit.aaf".
Whatever sequence ARC that the game references must also be included in that directory. You should drop the "Banks" and "Seqs" folder from the game into the same directory as well. 
Finally, the file you're playing needs to be in the same folder as BMSXPX, and named "test.bms". 

If you haven't extracted your sequences yet, BMSXPX will extract them for you and throw them into the seqs_out folder, but i have no promises of their validity.  I would highly advise using Arookas's FLAAFY instead. https://github.com/arookas/flaaffy/

In a pinch, your folder should look like this. 

(http://xayr.ga/share/01-2019/explorer_2019-01-13_04-34-48440ffa7e-c34f-4961-95fd-21cb14fbb519.png)[http://xayr.ga/share/01-2019/explorer_2019-01-13_04-34-48440ffa7e-c34f-4961-95fd-21cb14fbb519.png]








