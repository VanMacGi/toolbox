#!/bin/bash

# Function to display the help information
function display_help() {
    echo "Usage: $0 [options] <search_string>"
    echo "Options:"
    echo "  -s, --search-struct Search for defined structures containing the given string"
    echo "  -f, --search-func   Search for function definitions containing the given string"
    echo "  -F, --find-func     Search for the definition of a specific function"
    echo "  -S, --find-struct   Search for the definition of a specific structure"
    echo "  -g, --find-global   Search for the declaration of a specific global variable"
    echo "  -m, --find-macro    Search for the definition of a specific macro"
    echo "  -a, --all           Search for string in all .c and .h files"
    echo "  -d, --directory DIR Specify the directory to search for C source files (default: current directory)"
    echo "  -r, --recursive     Search for C source files recursively in subdirectories"
    echo "  -v, --verbose           Enable verbose mode to display detailed output"
    echo "  -h, --help              Display this help information"
}

# Function to search for defined structures containing the given string
function search_struct() {
    #>typedef struct struct_name {
    #>    ....
    #>} struct_other_name;

    #>struct struct_name {
    #>   ...
    #>};

    find $DIRECTORY -name "*.[ch]" | xargs \
    awk -v string=$1 '/^\s*(typedef )?struct[^=]*{/, /}/ {
        if (/^\s*(typedef )?struct[^=]*{/) {
            start_struct = "\n" FILENAME " +" FNR ":" $0
            has_string = 0
        } else if ($0 ~ string) {
            if (has_string == 0) {
                print start_struct
            }
            print FILENAME " +" FNR ":" $0
            has_string = 1
        } else if (/}/) {
            if (has_string == 1) {
                print FILENAME " +" FNR ":" $0
            }
        }
    }'
}

# Function to search for function definitions containing the given string
function search_func() {
    find $DIRECTORY -name "*.[ch]" | xargs \
    awk -v string="$1" '/^(static\s+)?(\w+\s+)(\*\s*)?\w+\s*\([^=<>]*(\))?\s*({)?\s*$/ && !/\selse\s/, /^}/ {
       if (/^(static\s+)?(\w+\s+)(\*\s*)?\w+\s*\([^=<>]*(\))?\s*({)?\s*$/ && !/\selse\s/) {
           start_string = "\n" FILENAME " +" FNR ":" $0
           if (!/{/)
               not_open = 1
           has_string = 0 
       } else if (not_open == 1) {
          start_string = start_string "\n" FILENAME " +" FNR ":" $0 
          if (/{/)
             not_open = 0
       } else if ($0 ~ string) {
           if (has_string == 0) {
               print start_string
           }
           print FILENAME " +" FNR ":" $0
           has_string = 1 
       } else if (/^}/ && has_string == 1) {
           print FILENAME " +" FNR ":" $0
       }   
   }'

}

# Function to search for the definition of a specific function
function find_func() {
    #[static] [type] [*] function_name(...)[{] 
    find $DIRECTORY -name "*.[ch]" | xargs \
    awk -v string="$1" -v content="$VERBOSE" '/^(static\s+)?(\w+\s+)(\*\s*)?\w+\s*\([^=<>]*\)\s*{?\s*$/, /^}/ {
        if (/^(static\s+)?(\w+\s+)(\*\s*)?\w+\s*\([^=<>]*\)\s*{?\s*$/ && $0 ~ string && !/;/ && /\(.*\)/) {
            print "\n" FILENAME " +" FNR ":" $0
            has_string = 1
        }
        else if (/^}/ && has_string == 1) {
            print FILENAME " +" FNR ":" $0
            has_string = 0
        }
        else if (has_string == 1 && content == "true") {
            print FILENAME " +" FNR ":" $0
        }
    }
    '

}

# Function to search for the definition of a specific structure
function find_struct() {
    find $DIRECTORY -name "*.[ch]" | xargs \
    awk -v string=$1 -v content="$VERBOSE" '/^(typedef )?struct[^=]*{/, /^}/ {
        if (/^(typedef )?struct[^=]*{/) {
            struct = "\n" FILENAME " +" FNR ":" $0
            if ($0 ~ string) {
                has_string = 1
            }
        } else if (/^}/ && ((has_string == 1) || ($0 ~ string ))) {
            print struct
            print FILENAME " +" FNR ":" $0
            has_string = 0
        } else if (content == "true") {
            struct = struct "\n" FILENAME " +" FNR ":" $0
        }
    }'
}

# Function to search for the declaration of a specific global variable
function find_global() {
    find $DIRECTORY -name "*.[ch]" | xargs \
    awk -v name=$1 '$0 ~ name && $0 !~ /\(/ && $0 !~ /\{/{
        if (/^\w.*/) {
            print FILENAME " +" FNR ":" $0
        }
    }'
}

# Function to search for the definition of a specific macro
function find_macro() {
    find $DIRECTORY -name "*.[ch]" | xargs \
    awk -v macro=$1 '/^[\s\t]*#[\s\t]*define[\s\t]*/ && $0 ~ macro {
        definition = FILENAME " +" FNR ":" $0
        while (/\\$/) {
            getline
            definition = definition "\n" FILENAME " +" FNR ":"  $0
        }
        print definition
        exit
    }'
}

# Function to search all string in all .h and .c files
function find_all() {
    find $DIRECTORY -name "*.[ch]" | xargs \
    awk -v string=$1 '$0 ~ string {print FILENAME " +" FNR ":" $0}'
}

# Check if there are no arguments provided, or if the help option is specified
if [[ $# -eq 0 || "$1" == "--help" ]]; then
    display_help
    exit 0
fi

# Default values for optional parameters
DIRECTORY=`cat /tmp/csearch.tmp`
[ "$DIRECTORY" == "" ] && DIRECTORY='./'
RECURSIVE="-maxdepth 1"
VERBOSE=false

# Parse the command-line arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        --search-struct|-S)
            search_struct "$2"
            exit 0
            ;;
        --search-func|-F)
            search_func "$2"
            exit 0
            ;;
        --find-func|-f)
            find_func "$2"
            exit 0
            ;;
        --find-struct|-s)
            find_struct "$2"
            exit 0
            ;;
        --find-global|-g)
            find_global "$2"
            exit 0
            ;;
        --find-macro|-m)
            find_macro "$2"
            exit 0
            ;;
        --find-all|-a)
            find_all "$2"
            exit 0
            ;;
        --directory|-d)
            is_set_directory=1
            DIRECTORY="$2"
            shift
            ;;
        --recursive|-r)
            RECURSIVE=
            ;;
        --verbose|-v)
            VERBOSE=true
            ;;
        *)
            echo "Error: Unknown option or invalid argument: $1"
            display_help
            exit 1
            ;;
    esac
    shift
done

[ "$is_set_directory" == "1" ] && echo $DIRECTORY > /tmp/csearch.tmp && exit 0

# If no valid option is provided, display the help
display_help
exit 1

