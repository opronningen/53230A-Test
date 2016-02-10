#53230A-Test
Various small specialized utilities to run particular experiments on the 53230A frequency counter. Of little interest to the general user, unless you want to replicate some of my experiments.

## Read3
1. Set the counter to frequency measurements, mode RCON or CONT, number of samples per trigger to n (\":samp:coun\"). :trig:sour IMM. 
2. Read3 sends a \"read?\", which triggers a new acquisition, and then logs each reading to a separate file. Number of files equals :samp:count
3. Check the mean frequency recorded in the first readings following a trigger (file 0.txt) vs the second (file 1.txt) vs the third etc. 

I find my counter has a clear tendency to report a slightly different (usually higher) frequency on the first reading following a trigger, and that this difference is highly temperature dependant. Odd.