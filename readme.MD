#53230A-Test
Various small specialized utilities to run particular experiments on the 53230A frequency counter. Of little interest to the general user, unless you want to replicate some of my experiments. This is very much a work in progress..

## Read3
Reads a number of samples from the counter in series of ":Samp:count". Splits each reading from the series into its own file: the first reading in a batch will go to 0.txt, the next reading will go to 1.txt and so on. If a separate query is specified, such as ":SYST:TEMP?", the query is run for each batch of readings, and the result logged to "q.txt"
1. Set the counter to frequency measurements, mode RCON or CONT, number of samples per trigger to n (\":samp:coun\"). :trig:sour IMM. 
2. Read3 sends a \"read?\", which triggers a new acquisition, and then logs each reading to a separate file. Number of files equals :samp:count
3. Check the mean frequency recorded in the first readings following a trigger (file 0.txt) vs the second (file 1.txt) vs the third etc. 

I find my counter has a clear tendency to report a slightly different (usually higher) frequency on the first reading following a trigger, and that this difference is highly temperature dependant. Odd.

## MA \<n\>
n - number of points to average over.

Straight up moving average filter. Does not output any samples untill the averaging window is filled. Usage:

R | MA 100 > "filtered output.txt"

