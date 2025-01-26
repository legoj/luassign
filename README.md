# luassign
Command-line utility to update MicroFocus Rumba profile's LU assignment. This was created to help automate the LU assignment during deployment as well as post-deployment changes without requiring the users of updating their profile manually.

**[Usage]**
-------------------
**Case 1**: *Dump BinaryData to binary file*

to generate <sessionFilePath>.dat file containing the binary data, use the following syntax:
  ```
 command:
   $>luassign <sessionFilePath>

 example:
   $>luassign "C:\Program Files (x86)\Micro Focus\RUMBA\Mframe\DISP1.rsdm"
```

**Case 2**: *Data index search*

to look for the offset index of the <searchString> in the binary data, use the following syntax:
```
 command:
   $>luassign <sessionFilePath> <searchString>

 example:
   $>luassign "C:\Program Files (x86)\Micro Focus\RUMBA\Mframe\DISP1.rsdm" R53AA38A
```

**Case 3**: *Create session file*

to creates a session file based on templates with the specified values

```
 command:
   $>luassign <sessionFilePath> <portNumber> <deviceName>

 example:
   $>luassign "C:\Program Files (x86)\Micro Focus\RUMBA\Mframe\DISP1.rsdm" 3233 R53AA38A
```

**Case 4**: *Create the session files for 2LU*

to create session files for 2LU on the specified output directory, use the following syntax. existing files are renamed as backup.

```
 command:
   $>luassign <outputDirectory> <portNumber> <dsp1Device> <prt1Device>

 example:
   $>luassign "C:\Program Files (x86)\Micro Focus\RUMBA\Mframe" 3233 R53AA38A R33AA38A
```

**Case 5**: *Create the session files for 3LU*

 to creates session files for 3LU on the specified output directory, use the following syntax. existing files are renamed as backup.
```
 command:
   $>luassign <outputDirectory> <portNumber> <dsp1Device> <dsp2Device> <prt1Device>

 example:
   $>luassign "C:\Program Files (x86)\Micro Focus\RUMBA\Mframe" 3233 R53AA38A R63AA38A R33AA38A
```
